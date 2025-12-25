using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCTV_Sentinel
{
    public class ScanResult
    {
        public string IP { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Vendor { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Credentials { get; set; } = string.Empty; // Stores discovered credentials
        public bool Success { get; set; }
        public string? Details { get; set; } // Optional diagnostic info
    }

    public sealed class ScannerEngine : IAsyncDisposable
    {
        private readonly int[] _ports;
        private readonly List<(string User, string Pass)> _commonCreds;
        private readonly HttpClient _httpClient;
        private readonly HttpClientHandler _httpHandler;
        private readonly int _maxParallel;
        private readonly TimeSpan _httpRequestTimeout;
        private readonly int _portProbeTimeoutMs;
        private readonly int _rtspConnectTimeoutMs;
        private bool _disposed;

        public ScannerEngine(
            IEnumerable<int>? ports = null,
            IEnumerable<(string User, string Pass)>? creds = null,
            int maxParallel = 50,
            TimeSpan? httpRequestTimeout = null,
            int portProbeTimeoutMs = 500,
            int rtspConnectTimeoutMs = 1000,
            bool acceptInvalidServerCertificates = true)
        {
            _ports = ports is not null ? new List<int>(ports).ToArray() : new[] { 80, 443, 554, 8000, 8080 };
            _commonCreds = creds is not null ? new List<(string, string)>(creds) : new List<(string, string)>
            {
                ("admin", "admin"),
                ("admin", "123456"),
                ("admin", "password"),
                ("root", "pass"),
                ("admin", "")
            };

            _maxParallel = Math.Max(1, maxParallel);
            _httpRequestTimeout = httpRequestTimeout ?? TimeSpan.FromSeconds(2);
            _portProbeTimeoutMs = Math.Max(50, portProbeTimeoutMs);
            _rtspConnectTimeoutMs = Math.Max(200, rtspConnectTimeoutMs);

            _httpHandler = new HttpClientHandler
            {
                UseCookies = false,
                // Keep redirects visible so login redirects are detectable
                AllowAutoRedirect = false
            };

            if (acceptInvalidServerCertificates)
            {
                _httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            _httpClient = new HttpClient(_httpHandler, disposeHandler: false)
            {
                Timeout = Timeout.InfiniteTimeSpan // we implement per-request timeout via CancellationTokenSource
            };
        }

        public async Task StartParallelScan(string subnet, IProgress<ScanResult> progress, CancellationToken ct)
        {
            ThrowIfDisposed();

            using var semaphore = new SemaphoreSlim(_maxParallel);
            var tasks = new List<Task>(254);

            for (int i = 1; i <= 254; i++)
            {
                if (ct.IsCancellationRequested) break;

                string ip = $"{subnet}.{i}";
                await semaphore.WaitAsync(ct).ConfigureAwait(false);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await ScanHost(ip, progress, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { /* respect cancellation */ }
                    catch (Exception ex)
                    {
                        // Report a non-fatal diagnostic result so caller can decide (keeps API minimal)
                        progress.Report(new ScanResult
                        {
                            IP = ip,
                            Port = 0,
                            Vendor = "ScannerEngine",
                            Status = "Scan Error",
                            Credentials = string.Empty,
                            Success = false,
                            Details = ex.Message
                        });
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, ct);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task ScanHost(string ip, IProgress<ScanResult> progress, CancellationToken ct)
        {
            ThrowIfDisposed();

            foreach (int port in _ports)
            {
                ct.ThrowIfCancellationRequested();

                bool open = await IsPortOpen(ip, port, ct).ConfigureAwait(false);
                if (!open) continue;

                ScanResult? result = null;

                if (port == 554)
                {
                    result = await TryRtspAuth(ip, port, ct).ConfigureAwait(false);
                }
                else
                {
                    // pass progress for diagnostics if needed
                    result = await TryHttpAuth(ip, port, progress, ct).ConfigureAwait(false);
                }

                if (result is not null && result.Success)
                {
                    progress.Report(result);
                    return; // Found a hit on this host, move to next IP
                }
            }
        }

        // Robust HTTP detection:
        // - Fetch page without auth once, then try each credential and compare status + content (small sample).
        // - Detect Basic auth by WWW-Authenticate/401 and treat success accordingly.
        // - Report diagnostic ScanResult entries (Success=false) when helpful.
        private async Task<ScanResult?> TryHttpAuth(string ip, int port, IProgress<ScanResult> progress, CancellationToken ct)
        {
            ThrowIfDisposed();

            string scheme = port == 443 ? "https" : "http";
            string url = $"{scheme}://{ip}:{port}/";

            // Get baseline (no auth) once
            byte[] noAuthSample = Array.Empty<byte>();
            HttpStatusCode? noAuthStatus = null;
            try
            {
                using var ctsNo = CancellationTokenSource.CreateLinkedTokenSource(ct);
                ctsNo.CancelAfter(_httpRequestTimeout);

                using var reqNo = new HttpRequestMessage(HttpMethod.Get, url);
                using var respNo = await _httpClient.SendAsync(reqNo, HttpCompletionOption.ResponseContentRead, ctsNo.Token).ConfigureAwait(false);
                noAuthStatus = respNo.StatusCode;
                var content = respNo.Content != null ? await respNo.Content.ReadAsByteArrayAsync(ctsNo.Token).ConfigureAwait(false) : Array.Empty<byte>();
                noAuthSample = TrimSample(content, 4096);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // baseline failed; continue trying credentials (some devices block unauth requests)
            }

            foreach (var (user, pass) in _commonCreds)
            {
                ct.ThrowIfCancellationRequested();

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(_httpRequestTimeout);

                try
                {
                    var reqWith = new HttpRequestMessage(HttpMethod.Get, url);
                    var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{pass}"));
                    reqWith.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

                    using var respWith = await _httpClient.SendAsync(reqWith, HttpCompletionOption.ResponseContentRead, cts.Token).ConfigureAwait(false);
                    var withStatus = respWith.StatusCode;
                    var withContent = respWith.Content != null ? await respWith.Content.ReadAsByteArrayAsync(cts.Token).ConfigureAwait(false) : Array.Empty<byte>();
                    var withSample = TrimSample(withContent, 4096);

                    // If server explicitly returns 401 -> credentials rejected
                    if (withStatus == HttpStatusCode.Unauthorized)
                        continue;

                    // If baseline was 401 but with-auth is success, treat as success
                    if (noAuthStatus == HttpStatusCode.Unauthorized && respWith.IsSuccessStatusCode)
                    {
                        return new ScanResult
                        {
                            IP = ip,
                            Port = port,
                            Vendor = respWith.Headers.Server?.ToString() ?? "Generic Web Server",
                            Status = "HTTP Authenticated",
                            Credentials = $"{user}:{pass}",
                            Success = true
                        };
                    }

                    // If response status differs or content differs, and with-auth is a success, treat as authenticated
                    bool contentDiffers = !AreSamplesEqual(noAuthSample, withSample);
                    if (respWith.IsSuccessStatusCode && (withStatus != noAuthStatus || contentDiffers))
                    {
                        return new ScanResult
                        {
                            IP = ip,
                            Port = port,
                            Vendor = respWith.Headers.Server?.ToString() ?? "Generic Web Server",
                            Status = "HTTP Authenticated",
                            Credentials = $"{user}:{pass}",
                            Success = true
                        };
                    }

                    // If we reach here, with-auth did not produce an obvious change.
                    // Report a diagnostic (non-success) so caller can inspect headers/content if desired.
                    progress.Report(new ScanResult
                    {
                        IP = ip,
                        Port = port,
                        Vendor = respWith.Headers.Server?.ToString() ?? "Generic Web Server",
                        Status = "Auth Attempted",
                        Credentials = $"{user}:{pass}",
                        Success = false,
                        Details = $"withStatus={withStatus}, baselineStatus={noAuthStatus}, contentDiff={contentDiffers}"
                    });
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // report transient diagnostic and try next credential
                    progress.Report(new ScanResult
                    {
                        IP = ip,
                        Port = port,
                        Vendor = "ScannerEngine",
                        Status = "HTTP Attempt Error",
                        Credentials = $"{user}:{pass}",
                        Success = false,
                        Details = ex.Message
                    });
                    continue;
                }
            }

            return null;
        }

        private static byte[] TrimSample(byte[] src, int max)
        {
            if (src == null || src.Length == 0) return Array.Empty<byte>();
            if (src.Length <= max) return src;
            var dst = new byte[max];
            Array.Copy(src, dst, max);
            return dst;
        }

        private static bool AreSamplesEqual(byte[] a, byte[] b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        private async Task<ScanResult?> TryRtspAuth(string ip, int port, CancellationToken ct)
        {
            ThrowIfDisposed();

            foreach (var (user, pass) in _commonCreds)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    using var client = new TcpClient();
                    var connectTask = client.ConnectAsync(ip, port);
                    var delayTask = Task.Delay(_rtspConnectTimeoutMs, ct);
                    var completed = await Task.WhenAny(connectTask, delayTask).ConfigureAwait(false);

                    if (completed != connectTask || !client.Connected)
                    {
                        // connect timed out or cancelled
                        continue;
                    }

                    using NetworkStream stream = client.GetStream();
                    // Use linked CTS for write/read timeout on this operation
                    using var opCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    opCts.CancelAfter(TimeSpan.FromMilliseconds(Math.Max(1000, _rtspConnectTimeoutMs)));

                    string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{pass}"));
                    var msg = Encoding.ASCII.GetBytes(
                        $"DESCRIBE rtsp://{ip}:{port} RTSP/1.0\r\n" +
                        "CSeq: 1\r\n" +
                        $"Authorization: Basic {auth}\r\n\r\n");

                    await stream.WriteAsync(msg.AsMemory(0, msg.Length), opCts.Token).ConfigureAwait(false);

                    var buffer = new byte[2048];
                    int read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), opCts.Token).ConfigureAwait(false);

                    if (read > 0)
                    {
                        string resp = Encoding.ASCII.GetString(buffer, 0, read);
                        if (resp.Contains("RTSP/1.0 200") || resp.Contains("RTSP/1.0 302"))
                        {
                            return new ScanResult
                            {
                                IP = ip,
                                Port = port,
                                Vendor = "RTSP Device",
                                Status = "RTSP Authenticated",
                                Credentials = $"{user}:{pass}",
                                Success = true
                            };
                        }
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception)
                {
                    // try next credential
                    continue;
                }
            }

            return null;
        }

        private async Task<bool> IsPortOpen(string ip, int port, CancellationToken ct)
        {
            ThrowIfDisposed();

            try
            {
                using var tcp = new TcpClient();
                var connectTask = tcp.ConnectAsync(ip, port);
                var delayTask = Task.Delay(_portProbeTimeoutMs, ct);
                var completed = await Task.WhenAny(connectTask, delayTask).ConfigureAwait(false);

                return completed == connectTask && tcp.Connected;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return false;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ScannerEngine));
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            _disposed = true;

            // dispose managed resources
            _httpClient.Dispose();
            _httpHandler.Dispose();

            await Task.CompletedTask;
        }
    }
}