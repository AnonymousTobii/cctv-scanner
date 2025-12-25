
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CCTV_Sentinel
{
    public partial class Form1 : Form
    {
        private CancellationTokenSource? _cts;
        private readonly ScannerEngine _engine;
        private readonly HashSet<string> _reportedIps = new();

        public Form1()
        {
            InitializeComponent();

            // Create the engine with reasonable defaults (can be changed later)
            _engine = new ScannerEngine(
                maxParallel: 50,
                httpRequestTimeout: TimeSpan.FromSeconds(3),
                portProbeTimeoutMs: 500,
                rtspConnectTimeoutMs: 1000,
                acceptInvalidServerCertificates: true
            );

            SetupGrid();

            // Ensure we dispose engine when form is closed
            this.FormClosed += Form1_FormClosedAsync;
        }

        private void SetupGrid()
        {
            dgvResults.RowHeadersVisible = false;
            dgvResults.AllowUserToAddRows = false;
            dgvResults.Rows.Clear();
            dgvResults.Columns.Clear();

            dgvResults.Columns.Add("IP", "Target IP");
            dgvResults.Columns.Add("Port", "Port(s)");
            dgvResults.Columns.Add("Vendor", "Vendor");
            dgvResults.Columns.Add("Status", "Status");
            dgvResults.Columns.Add("Credentials", "Credentials");

            dgvResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtSubnet.Text = "192.168.1";
            lblStatus.Text = "Ready";
        }

        // Ensure engine is disposed when the form is closed.
        // Async void is acceptable for event handlers; keep this short and safe.
        private async void Form1_FormClosedAsync(object? sender, FormClosedEventArgs e)
        {
            try
            {
                _cts?.Cancel();
                if (_engine != null)
                {
                    await _engine.DisposeAsync();
                }
            }
            catch
            {
                // swallow - closing the form, nothing to do
            }
        }

        private async void btnStart_Click_1(object sender, EventArgs e)
        {

            // Prevent double-start
            if (_cts != null) return;

            _cts = new CancellationTokenSource();
            _reportedIps.Clear();

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            btnStart.Text = "Scanning...";
            dgvResults.Rows.Clear();
            lblStatus.Text = $"Scanning {txtSubnet.Text}.x...";

            int successCount = 0;
            int scannedCount = 0;

            var progress = new Progress<ScanResult>(result =>
            {
                scannedCount++;

                // If we've already reported this exact (IP,Port,Credentials) we avoid duplicate rows.
                // Keep a single row per IP but aggregate ports.
                bool updated = false;
                for (int r = 0; r < dgvResults.Rows.Count; r++)
                {
                    var cellIp = dgvResults.Rows[r].Cells["IP"].Value?.ToString();
                    if (string.Equals(cellIp, result.IP, StringComparison.OrdinalIgnoreCase))
                    {
                        // Append port if it's not already present in the Port(s) cell
                        var portCell = dgvResults.Rows[r].Cells["Port"];
                        var existingPorts = portCell.Value?.ToString() ?? string.Empty;
                        if (!existingPorts.Contains(result.Port.ToString()))
                        {
                            portCell.Value = string.IsNullOrEmpty(existingPorts) ? result.Port.ToString() : $"{existingPorts}, {result.Port}";
                        }

                        // Update vendor/status/credentials if this result indicates success or new info
                        if (!string.IsNullOrEmpty(result.Vendor))
                            dgvResults.Rows[r].Cells["Vendor"].Value = result.Vendor;

                        dgvResults.Rows[r].Cells["Status"].Value = result.Status ?? dgvResults.Rows[r].Cells["Status"].Value;
                        if (!string.IsNullOrEmpty(result.Credentials))
                            dgvResults.Rows[r].Cells["Credentials"].Value = result.Credentials;

                        // Visual treatment for successful auth
                        if (result.Success)
                        {
                            dgvResults.Rows[r].DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 48);
                            dgvResults.Rows[r].DefaultCellStyle.ForeColor = Color.LimeGreen;
                            dgvResults.Rows[r].Cells["Credentials"].Style.ForeColor = Color.Yellow;
                            lblStatus.Text = $"Auth Success: {result.IP}";
                            successCount++;
                        }

                        updated = true;
                        break;
                    }
                }

                if (!updated)
                {
                    int rowIndex = dgvResults.Rows.Add(
                        result.IP,
                        result.Port,
                        result.Vendor,
                        result.Status,
                        string.IsNullOrEmpty(result.Credentials) ? "N/A" : result.Credentials
                    );

                    if (result.Success)
                    {
                        dgvResults.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 48);
                        dgvResults.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.LimeGreen;
                        if (!string.IsNullOrEmpty(result.Credentials))
                            dgvResults.Rows[rowIndex].Cells["Credentials"].Style.ForeColor = Color.Yellow;

                        lblStatus.Text = $"Auth Success: {result.IP}";
                        successCount++;
                    }
                }

                // Simple status update (scan progress is approximate)
                lblStatus.Text = $"Scanning {txtSubnet.Text}.x... Scanned {scannedCount} hosts, {successCount} successes";
            });

            try
            {
                await _engine.StartParallelScan(txtSubnet.Text, progress, _cts.Token);
                lblStatus.Text = $"Scan Complete. {dgvResults.Rows.Count} hosts seen, {successCount} successes.";
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Scan Cancelled.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
            }
            finally
            {
                _cts.Dispose();
                _cts = null;

                btnStart.Enabled = true;
                btnStop.Enabled = false;
                btnStart.Text = "Start Scan";
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
            btnStop.Enabled = false;
            lblStatus.Text = "Stopping scan...";
        }
    }
}
