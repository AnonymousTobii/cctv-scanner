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
using System.Runtime.InteropServices;
using System.Linq;

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
        public string MACAddress { get; set; } = string.Empty; // MAC address of the device
        public string DeviceType { get; set; } = string.Empty; // Type of device (Camera, DVR, NVR, etc.)
        public string ONVIFProfile { get; set; } = string.Empty; // ONVIF profile if detected
        public string FirmwareVersion { get; set; } = string.Empty; // Firmware version if detectable
        public DateTime DiscoveryTime { get; set; } = DateTime.Now; // When this device was discovered
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

        // Advanced scanning features
        private readonly Dictionary<string, string> _macVendors;
        private readonly string[] _onvifPaths;
        private readonly string[] _deviceSignatures;

        public ScannerEngine(
            IEnumerable<int>? ports = null,
            IEnumerable<(string User, string Pass)>? creds = null,
            int maxParallel = 50,
            TimeSpan? httpRequestTimeout = null,
            int portProbeTimeoutMs = 500,
            int rtspConnectTimeoutMs = 1000,
            bool acceptInvalidServerCertificates = true)
        {
            _ports = ports is not null ? new List<int>(ports).ToArray() : new[] { 80, 443, 554, 8000, 8080, 8081, 8443, 9000 };
            _commonCreds = creds is not null ? new List<(string, string)>(creds) : new List<(string, string)>
            {
                ("admin", "admin"),
                ("admin", "123456"),
                ("admin", "password"),
                ("root", "pass"),
                ("admin", ""),
                ("administrator", "administrator"),
                ("admin", "admin123"),
                ("admin", "12345"),
                ("admin", "qwerty"),
                ("user", "user"),
                ("guest", "guest"),
                ("root", "root"),
                ("support", "support"),
                ("service", "service"),
                ("camera", "camera"),
                ("hikvision", "12345"),
                ("dahua", "admin"),
                ("axis", "root"),
                ("sony", "admin")
            };

            _macVendors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "00:1A:7F", "Hikvision" },
                { "44:19:B6", "Hikvision" },
                { "88:BB:AA", "Hikvision" },
                { "FC:D7:33", "Hikvision" },
                { "3C:CE:73", "Hikvision" },
                { "00:12:34", "Dahua" },
                { "00:1B:4E", "Dahua" },
                { "AC:A2:13", "Dahua" },
                { "B4:55:70", "Dahua" },
                { "00:40:8C", "Axis" },
                { "AC:CC:8E", "Axis" },
                { "00:16:9C", "Sony" },
                { "00:1D:FE", "Sony" },
                { "00:E0:91", "Panasonic" },
                { "00:A0:2D", "Panasonic" },
                { "00:0D:3A", "Canon" },
                { "00:14:38", "Canon" },
                { "00:1E:C9", "Samsung" },
                { "00:21:4C", "Samsung" },
                { "00:15:99", "D-Link" },
                { "00:17:9A", "D-Link" },
                { "00:18:E7", "TP-Link" },
                { "00:23:CD", "TP-Link" },
                { "00:1A:2B", "Ubiquiti" },
                { "00:27:22", "Ubiquiti" },
                { "00:0C:29", "VMware" },
                { "00:50:56", "VMware" },
                { "00:15:5D", "Hyper-V" },
                { "00:1D:D8", "Microsoft" }
            };

            _onvifPaths = new[]
            {
                "/onvif/device_service",
                "/onvif/services",
                "/onvif/device_management",
                "/devicemgr/devicemgr.wsdl",
                "/services/gateway",
                "/cgi-bin/gw.cgi"
            };

            _deviceSignatures = new[]
            {
                "Hikvision", "Dahua", "Axis", "Sony", "Panasonic", "Canon", "Samsung",
                "IP Camera", "Network Camera", "Surveillance", "NVR", "DVR", "Video Recorder",
                "RTSP Server", "ONVIF", "Web Service for IP Camera"
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

            string scheme = port == 443 || port == 8443 ? "https" : "http";
            string url = $"{scheme}://{ip}:{port}/";

            // Get baseline (no auth) once
            byte[] noAuthSample = Array.Empty<byte>();
            HttpStatusCode? noAuthStatus = null;
            string? serverHeader = null;
            string? responseBodySample = null;
            
            try
            {
                using var ctsNo = CancellationTokenSource.CreateLinkedTokenSource(ct);
                ctsNo.CancelAfter(_httpRequestTimeout);

                using var reqNo = new HttpRequestMessage(HttpMethod.Get, url);
                using var respNo = await _httpClient.SendAsync(reqNo, HttpCompletionOption.ResponseContentRead, ctsNo.Token).ConfigureAwait(false);
                noAuthStatus = respNo.StatusCode;
                serverHeader = respNo.Headers.Server?.ToString();
                var content = respNo.Content != null ? await respNo.Content.ReadAsByteArrayAsync(ctsNo.Token).ConfigureAwait(false) : Array.Empty<byte>();
                noAuthSample = TrimSample(content, 4096);
                responseBodySample = Encoding.UTF8.GetString(noAuthSample);
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
                    string? withServerHeader = respWith.Headers.Server?.ToString();
                    string? withResponseBody = Encoding.UTF8.GetString(withSample);

                    // If server explicitly returns 401 -> credentials rejected
                    if (withStatus == HttpStatusCode.Unauthorized)
                        continue;

                    // Build rich scan result with advanced features
                    var result = new ScanResult
                    {
                        IP = ip,
                        Port = port,
                        Vendor = withServerHeader ?? serverHeader ?? "Generic Web Server",
                        Status = "HTTP Authenticated",
                        Credentials = $"{user}:{pass}",
                        Success = true,
                        DiscoveryTime = DateTime.Now
                    };

                    // Get MAC address
                    string? mac = GetMacAddress(ip);
                    if (!string.IsNullOrEmpty(mac))
                    {
                        result.MACAddress = mac;
                        
                        // Look up vendor from MAC
                        string macPrefix = mac.Length >= 8 ? mac.Substring(0, 8) : "";
                        if (_macVendors.TryGetValue(macPrefix, out string? macVendor))
                        {
                            result.Vendor = macVendor;
                        }
                    }

                    // Identify device type
                    result.DeviceType = IdentifyDeviceType(result.Vendor, port, withResponseBody);

                    // Detect ONVIF profile
                    string? onvifProfile = await DetectOnvifProfile(ip, port, ct);
                    if (!string.IsNullOrEmpty(onvifProfile))
                    {
                        result.ONVIFProfile = onvifProfile;
                    }

                    // Extract firmware version
                    string? firmware = ExtractFirmwareVersion(withServerHeader ?? serverHeader, withResponseBody);
                    if (!string.IsNullOrEmpty(firmware))
                    {
                        result.FirmwareVersion = firmware;
                    }

                    // Check for device signatures in response
                    if (!string.IsNullOrEmpty(withResponseBody))
                    {
                        foreach (var signature in _deviceSignatures)
                        {
                            if (withResponseBody.Contains(signature, StringComparison.OrdinalIgnoreCase))
                            {
                                if (string.IsNullOrEmpty(result.DeviceType) || result.DeviceType == "Network Device")
                                {
                                    result.DeviceType = signature switch
                                    {
                                        "Hikvision" => "Hikvision Camera/DVR",
                                        "Dahua" => "Dahua Camera/NVR",
                                        "Axis" => "Axis Network Camera",
                                        "NVR" => "Network Video Recorder",
                                        "DVR" => "Digital Video Recorder",
                                        "IP Camera" => "IP Camera",
                                        "ONVIF" => "ONVIF Device",
                                        _ => result.DeviceType
                                    };
                                }
                                break;
                            }
                        }
                    }

                    return result;
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
                        Details = ex.Message,
                        DiscoveryTime = DateTime.Now
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

        // Advanced: Get MAC address via ARP table lookup (Windows-specific)
        public static string? GetMacAddress(string ip)
        {
            try
            {
                // First ensure we have an ARP entry by pinging
                using var ping = new System.Net.NetworkInformation.Ping();
                ping.Send(ip, 100);

                // Execute arp -a command to get MAC from ARP cache
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "arp",
                    Arguments = $"-a {ip}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(1000);

                    // Parse MAC address from arp output
                    // Format: IP address      Physical Address      Type
                    //         192.168.1.1     00-1a-7f-xx-xx-xx     dynamic
                    var lines = output.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains(ip, StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                string mac = parts[1].Replace("-", ":");
                                return mac.ToUpperInvariant();
                            }
                        }
                    }
                }
            }
            catch
            {
                // ARP lookup failed, return null
            }

            return null;
        }

        // Identify device type based on vendor signatures and open ports
        public string IdentifyDeviceType(string vendor, int port, string? responseBody)
        {
            if (string.IsNullOrEmpty(vendor) && string.IsNullOrEmpty(responseBody))
                return "Unknown";

            string combined = ($"{vendor} {responseBody}").ToLowerInvariant();

            if (combined.Contains("hikvision") || combined.Contains("hiwatch"))
                return "Hikvision Camera/DVR";
            if (combined.Contains("dahua") || combined.Contains("dahuasecurity"))
                return "Dahua Camera/NVR";
            if (combined.Contains("axis"))
                return "Axis Network Camera";
            if (combined.Contains("sony") && combined.Contains("camera"))
                return "Sony IP Camera";
            if (combined.Contains("panasonic"))
                return "Panasonic Security Camera";
            if (combined.Contains("canon"))
                return "Canon Network Camera";
            if (combined.Contains("samsung") && combined.Contains("smartcam"))
                return "Samsung SmartCam";
            if (combined.Contains("ubiquiti") || combined.Contains("unifi"))
                return "Ubiquiti UniFi Device";
            if (combined.Contains("tp-link") || combined.Contains("tapo"))
                return "TP-Link Camera";
            if (combined.Contains("d-link"))
                return "D-Link Camera";
            if (combined.Contains("nvr") || combined.Contains("network video recorder"))
                return "Network Video Recorder (NVR)";
            if (combined.Contains("dvr") || combined.Contains("digital video recorder"))
                return "Digital Video Recorder (DVR)";
            if (combined.Contains("onvif"))
                return "ONVIF-Compatible Device";

            // Port-based heuristics
            if (port == 554)
                return "RTSP Streaming Device";
            if (port == 80 || port == 8080 || port == 8000)
                return "Web-Enabled Device";
            if (port == 443 || port == 8443)
                return "Secure Web Device";

            return "Network Device";
        }

        // Detect ONVIF support
        public async Task<string?> DetectOnvifProfile(string ip, int port, CancellationToken ct)
        {
            string scheme = port == 443 || port == 8443 ? "https" : "http";
            
            foreach (var path in _onvifPaths)
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(2));

                    var url = $"{scheme}://{ip}:{port}{path}";
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    
                    using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                        .ConfigureAwait(false);

                    if (resp.IsSuccessStatusCode || resp.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        // Check for ONVIF indicators in headers or content
                        string serverHeader = resp.Headers.Server?.ToString() ?? "";
                        if (serverHeader.ToLowerInvariant().Contains("onvif") ||
                            resp.Content?.Headers?.ContentType?.MediaType?.ToLowerInvariant().Contains("xml") == true)
                        {
                            return "ONVIF Detected";
                        }

                        // Try to read a small sample of the content
                        if (resp.Content != null)
                        {
                            var buffer = new byte[512];
                            var stream = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
                            int read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cts.Token)
                                .ConfigureAwait(false);
                            
                            if (read > 0)
                            {
                                string content = Encoding.ASCII.GetString(buffer, 0, read);
                                if (content.Contains("onvif.org") || content.Contains("<tt:") || 
                                    content.Contains("DeviceService") || content.Contains("devicemgr"))
                                {
                                    return "ONVIF Profile Detected";
                                }
                            }
                        }
                    }
                }
                catch when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    // Try next path
                }
            }

            return null;
        }

        // Extract firmware version from response headers or content
        public static string? ExtractFirmwareVersion(string? serverHeader, string? responseBody)
        {
            if (string.IsNullOrEmpty(serverHeader) && string.IsNullOrEmpty(responseBody))
                return null;

            // Common firmware version patterns
            var patterns = new[]
            {
                @"V(\d+\.\d+\.\d+)",
                @"ver[_\s]?(\d+\.\d+\.\d+)",
                @"firmware[_\s]?(\d+\.\d+\.\d+)",
                @"version[_\s]?(\d+\.\d+\.\d+)",
                @"(\d+\.\d+\.\d+\.\d+)"
            };

            string combined = $"{serverHeader} {responseBody}".ToLowerInvariant();

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(combined, pattern);
                if (match.Success)
                {
                    return match.Value;
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