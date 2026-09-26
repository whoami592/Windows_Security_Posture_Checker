using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Sabaz.SecurityPosture
{
    public sealed class MainForm : Form
    {
        private readonly Button scan = new Button { Text = "Scan this computer", AutoSize = true };
        private readonly Button cancel = new Button { Text = "Cancel", AutoSize = true, Enabled = false };
        private readonly Button export = new Button { Text = "Export full report", AutoSize = true, Enabled = false };
        private readonly ComboBox filter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
        private readonly TextBox search = new TextBox { Width = 200 };
        private readonly Label summary = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
        private readonly Label status = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, Text = "Ready. Select Scan this computer." };
        private readonly DataGridView grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoGenerateColumns = false, RowHeadersVisible = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White, BorderStyle = BorderStyle.None };
        private readonly TextBox details = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.White, Text = "Select a finding to view its evidence and guidance." };
        private readonly ProgressBar progress = new ProgressBar { Dock = DockStyle.Fill };
        private ScanReport report;
        private CancellationTokenSource cancellation;
        private bool closing;
        public MainForm()
        {
            Text = "Windows Security Posture Checker | Mr Sabaz Ali Khan";
            Size = new Size(1180, 800); MinimumSize = new Size(900, 650);
            StartPosition = FormStartPosition.CenterScreen; AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 10); BackColor = Color.FromArgb(242, 246, 251);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 8, Padding = new Padding(18) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 148));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            Controls.Add(layout);
            var heading = new Label { Text = "WINDOWS SECURITY POSTURE CHECKER\nCoded by Cyber Security Engineer Mr Sabaz Ali Khan", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(23, 51, 83) };
            layout.Controls.Add(heading, 0, 0);
            layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Local, read-only security evidence. No settings are changed.\nPASS = named check satisfied; WARNING = concern; REVIEW = context needed; UNKNOWN = not verified; INFO = evidence only." }, 0, 1);
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
            toolbar.Controls.Add(scan); toolbar.Controls.Add(cancel); toolbar.Controls.Add(export);
            toolbar.Controls.Add(new Label { Text = "Filter", AutoSize = true, Margin = new Padding(10, 8, 3, 0) });
            filter.Items.AddRange(new object[] { "ALL", "WARNING", "REVIEW", "UNKNOWN", "PASS", "INFO" }); filter.SelectedIndex = 0;
            toolbar.Controls.Add(filter);
            toolbar.Controls.Add(new Label { Text = "Search", AutoSize = true, Margin = new Padding(10, 8, 3, 0) }); toolbar.Controls.Add(search);
            layout.Controls.Add(toolbar, 0, 2); layout.Controls.Add(summary, 0, 3);
            AddColumn("Title", "Security check", 26); AddColumn("Status", "Status", 12); AddColumn("Evidence", "Observed evidence", 62);
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ColumnHeadersHeight = 35; grid.RowTemplate.Height = 32;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(244, 247, 251);
            layout.Controls.Add(grid, 0, 4); layout.Controls.Add(details, 0, 5); layout.Controls.Add(progress, 0, 6); layout.Controls.Add(status, 0, 7);
            scan.Click += async delegate { await ScanAsync(); };
            cancel.Click += delegate { if (cancellation != null) { cancel.Enabled = false; cancellation.Cancel(); status.Text = "Cancelling..."; } };
            export.Click += delegate { SaveReport(); };
            filter.SelectedIndexChanged += delegate { Render(); }; search.TextChanged += delegate { Render(); };
            grid.SelectionChanged += delegate { ShowDetails(); };
            FormClosing += delegate(object sender, FormClosingEventArgs e) {
                if (cancellation != null) { e.Cancel = true; closing = true; cancellation.Cancel(); status.Text = "Stopping collector before closing..."; }
            };
        }
        private void AddColumn(string property, string caption, int weight)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = property, HeaderText = caption, FillWeight = weight, SortMode = DataGridViewColumnSortMode.NotSortable });
        }
        private async System.Threading.Tasks.Task ScanAsync()
        {
            cancellation = new CancellationTokenSource();
            scan.Enabled = false; cancel.Enabled = true; export.Enabled = false;
            report = null; grid.DataSource = null; summary.Text = "Collecting local evidence...";
            details.Text = "Some checks require administrator privileges. Unavailable evidence is reported as UNKNOWN.";
            progress.Style = ProgressBarStyle.Marquee; status.Text = "Scanning. Overall timeout: 120 seconds.";
            try
            {
                report = await Collector.RunAsync(cancellation.Token);
                Render(); export.Enabled = true;
                status.Text = "Scan complete. UTC: " + report.CollectedUtc + " | Elevated: " + report.Elevated;
            }
            catch (OperationCanceledException) { status.Text = "Scan cancelled. No complete report was produced."; summary.Text = "Cancelled"; }
            catch (Exception ex) { status.Text = "Scan failed."; summary.Text = "No verified results"; details.Text = ex.Message + "\r\n\r\nCheck Windows PowerShell and your organization's application-control policy. Do not disable security controls to run this app."; }
            finally
            {
                progress.Style = ProgressBarStyle.Blocks; progress.Value = 0;
                scan.Enabled = true; cancel.Enabled = false; cancellation.Dispose(); cancellation = null;
                if (closing) Close();
            }
        }
        private void Render()
        {
            if (report == null) return;
            string query = search.Text.Trim(); string state = Convert.ToString(filter.SelectedItem);
            var findings = report.Items.Where(f => (state == "ALL" || f.Status == state) &&
                (String.IsNullOrEmpty(query) || ((f.Title ?? "") + " " + (f.Evidence ?? "") + " " + (f.Advice ?? "")).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            grid.DataSource = findings;
            foreach (DataGridViewRow row in grid.Rows)
            {
                var f = row.DataBoundItem as Finding;
                if (f != null) row.Cells[1].Style.ForeColor = f.Status == "PASS" ? Color.DarkGreen : f.Status == "WARNING" ? Color.Firebrick : f.Status == "REVIEW" ? Color.DarkGoldenrod : Color.DimGray;
            }
            summary.Text = String.Join("   |   ", new[] { "PASS", "WARNING", "REVIEW", "UNKNOWN", "INFO" }.Select(s => s + ": " + report.Items.Count(f => f.Status == s)))
                + "\nShowing " + findings.Count + " / " + report.Items.Count + " checks. No overall security or compliance score is assigned.";
            ShowDetails();
        }
        private void ShowDetails()
        {
            var f = grid.CurrentRow == null ? null : grid.CurrentRow.DataBoundItem as Finding;
            details.Text = f == null ? "No finding selected." : f.Title + " [" + f.Status + "]\r\n\r\nEvidence: " + f.Evidence + "\r\n\r\nGuidance: " + f.Advice;
        }
        private void SaveReport()
        {
            if (report == null) return;
            using (var dialog = new SaveFileDialog { Title = "Export all findings (filters do not limit export)", Filter = "HTML report (*.html)|*.html|CSV spreadsheet (*.csv)|*.csv|JSON evidence (*.json)|*.json", FileName = "Security_Posture_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"), AddExtension = true, DefaultExt = "html", OverwritePrompt = true })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    string content = dialog.FilterIndex == 1 ? Export.Html(report) : dialog.FilterIndex == 2 ? Export.Csv(report) : ReportCodec.Serializer().Serialize(report);
                    Export.Save(dialog.FileName, content); status.Text = "Saved full report: " + dialog.FileName;
                }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }
}
