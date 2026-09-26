using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sabaz.SecurityPosture
{
    public static class Collector
    {
        public static async Task<ScanReport> RunAsync(CancellationToken token)
        {
            string script;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Sabaz.Collect.ps1"))
            {
                if (stream == null) throw new InvalidOperationException("Embedded collector is missing. Rebuild the project.");
                using (var reader = new StreamReader(stream)) script = reader.ReadToEnd();
            }
            string exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe");
            if (!File.Exists(exe)) throw new FileNotFoundException("Windows PowerShell is unavailable.");
            var start = new ProcessStartInfo(exe) {
                Arguments = "-NoLogo -NoProfile -NonInteractive -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)),
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
            };
            if (start.Arguments.Length > 30000) throw new InvalidOperationException("Embedded script exceeds the process command limit.");
            using (var process = new Process { StartInfo = start })
            {
                token.ThrowIfCancellationRequested();
                process.Start();
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                var timer = Stopwatch.StartNew();
                try
                {
                    while (!process.HasExited)
                    {
                        token.ThrowIfCancellationRequested();
                        if (timer.Elapsed > TimeSpan.FromSeconds(120)) throw new TimeoutException("Scan exceeded 120 seconds. Check local PowerShell/CIM availability and try again.");
                        await Task.Delay(150, token).ConfigureAwait(false);
                    }
                    string json = await output.ConfigureAwait(false);
                    string diagnostics = await error.ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    if (process.ExitCode != 0 || String.IsNullOrWhiteSpace(json))
                        throw new InvalidOperationException("Evidence collection failed. " + diagnostics);
                    return ReportCodec.Parse(json);
                }
                finally
                {
                    if (!process.HasExited) { try { process.Kill(); process.WaitForExit(3000); } catch (InvalidOperationException) { } }
                }
            }
        }
    }
}
