
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;

namespace CCTV_Sentinel
{
    public partial class Form1 : Form
    {
        private CancellationTokenSource? _cts;
        private readonly ScannerEngine _engine;
        private readonly HashSet<string> _reportedIps = new();
        private readonly List<ScanResult> _scanResults = new();

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
            dgvResults.Columns.Add("MAC", "MAC Address");
            dgvResults.Columns.Add("Port", "Port(s)");
            dgvResults.Columns.Add("DeviceType", "Device Type");
            dgvResults.Columns.Add("Vendor", "Vendor");
            dgvResults.Columns.Add("Status", "Status");
            dgvResults.Columns.Add("Credentials", "Credentials");
            dgvResults.Columns.Add("ONVIF", "ONVIF Support");
            dgvResults.Columns.Add("Firmware", "Firmware Version");

            dgvResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            
            // Style configuration
            dgvResults.DefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
            dgvResults.DefaultCellStyle.ForeColor = Color.White;
            dgvResults.DefaultCellStyle.SelectionBackColor = Color.FromArgb(50, 50, 50);
            dgvResults.DefaultCellStyle.SelectionForeColor = Color.LimeGreen;
            dgvResults.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40);
            dgvResults.ColumnHeadersDefaultCellStyle.ForeColor = Color.LimeGreen;
            dgvResults.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvResults.EnableHeadersVisualStyles = false;
            dgvResults.GridColor = Color.FromArgb(60, 60, 60);
            dgvResults.BorderStyle = BorderStyle.None;
            dgvResults.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtSubnet.Text = "192.168.1";
            lblStatus.Text = "Ready";
            lblProgress.Text = "";
            
            // Set up modern UI styling
            this.BackColor = Color.FromArgb(20, 20, 20);
            this.ForeColor = Color.White;
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
            _scanResults.Clear();

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            btnStart.Text = "Scanning...";
            dgvResults.Rows.Clear();
            lblStatus.Text = $"Scanning {txtSubnet.Text}.x...";
            lblProgress.Text = "Initializing scan...";

            int successCount = 0;
            int scannedCount = 0;

            var progress = new Progress<ScanResult>(result =>
            {
                scannedCount++;

                // Update all results list
                _scanResults.Add(result);

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

                        if (!string.IsNullOrEmpty(result.DeviceType))
                            dgvResults.Rows[r].Cells["DeviceType"].Value = result.DeviceType;

                        if (!string.IsNullOrEmpty(result.MACAddress))
                            dgvResults.Rows[r].Cells["MAC"].Value = result.MACAddress;

                        if (!string.IsNullOrEmpty(result.ONVIFProfile))
                            dgvResults.Rows[r].Cells["ONVIF"].Value = result.ONVIFProfile;

                        if (!string.IsNullOrEmpty(result.FirmwareVersion))
                            dgvResults.Rows[r].Cells["Firmware"].Value = result.FirmwareVersion;

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

                if (!updated && result.Success)
                {
                    int rowIndex = dgvResults.Rows.Add(
                        result.IP,
                        result.MACAddress,
                        result.Port,
                        result.DeviceType,
                        result.Vendor,
                        result.Status,
                        string.IsNullOrEmpty(result.Credentials) ? "N/A" : result.Credentials,
                        result.ONVIFProfile,
                        result.FirmwareVersion
                    );

                    dgvResults.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 48);
                    dgvResults.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.LimeGreen;
                    if (!string.IsNullOrEmpty(result.Credentials))
                        dgvResults.Rows[rowIndex].Cells["Credentials"].Style.ForeColor = Color.Yellow;

                    lblStatus.Text = $"Auth Success: {result.IP}";
                    successCount++;
                }

                // Simple status update (scan progress is approximate)
                lblProgress.Text = $"Scanned: {scannedCount} | Successes: {successCount} | Total Rows: {dgvResults.Rows.Count}";
            });

            try
            {
                await _engine.StartParallelScan(txtSubnet.Text, progress, _cts.Token);
                lblStatus.Text = $"Scan Complete. {dgvResults.Rows.Count} hosts seen, {successCount} successes.";
                lblProgress.Text = $"Total scan results: {_scanResults.Count}";
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

        private void btnExportJson_Click(object sender, EventArgs e)
        {
            if (_scanResults.Count == 0)
            {
                MessageBox.Show("No scan results to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                FileName = $"CCTV_Scan_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                Title = "Export Scan Results as JSON"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var jsonSettings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented,
                        NullValueHandling = NullValueHandling.Ignore
                    };
                    
                    string json = JsonConvert.SerializeObject(_scanResults, jsonSettings);
                    File.WriteAllText(saveDialog.FileName, json);
                    MessageBox.Show($"Results exported successfully to:\n{saveDialog.FileName}", 
                        "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Success);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Export Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnExportCsv_Click(object sender, EventArgs e)
        {
            if (_scanResults.Count == 0)
            {
                MessageBox.Show("No scan results to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = $"CCTV_Scan_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "Export Scan Results as CSV"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var csvLines = new List<string>();
                    csvLines.Add("IP,MAC,Port,DeviceType,Vendor,Status,Credentials,ONVIF,Firmware,DiscoveryTime");

                    foreach (var result in _scanResults)
                    {
                        string line = $"\"{result.IP}\"," +
                                     $"\"{result.MACAddress}\"," +
                                     $"{result.Port}," +
                                     $"\"{result.DeviceType}\"," +
                                     $"\"{result.Vendor}\"," +
                                     $"\"{result.Status}\"," +
                                     $"\"{result.Credentials}\"," +
                                     $"\"{result.ONVIFProfile}\"," +
                                     $"\"{result.FirmwareVersion}\"," +
                                     $"\"{result.DiscoveryTime:yyyy-MM-dd HH:mm:ss}\"";
                        csvLines.Add(line);
                    }

                    File.WriteAllLines(saveDialog.FileName, csvLines);
                    MessageBox.Show($"Results exported successfully to:\n{saveDialog.FileName}",
                        "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Success);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnClearResults_Click(object sender, EventArgs e)
        {
            dgvResults.Rows.Clear();
            _scanResults.Clear();
            lblStatus.Text = "Results cleared";
            lblProgress.Text = "";
        }
    }
}
