using System;
using System.IO;
using System.Net;
using System.Text;

namespace Sabaz.SecurityPosture
{
    public static class Export
    {
        public static string CsvCell(string value)
        {
            value = value ?? "";
            if (value.Length > 0 && "=+-@\t\r\n".IndexOf(value[0]) >= 0) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        public static string Csv(ScanReport report)
        {
            var b = new StringBuilder("Machine,CollectedUtc,Elevated,Id,Check,Status,Evidence,Guidance\r\n");
            foreach (var f in report.Items)
            {
                string[] cells = { report.Machine, report.CollectedUtc, report.Elevated.ToString(), f.Id, f.Title, f.Status, f.Evidence, f.Advice };
                for (int i = 0; i < cells.Length; i++) { if (i > 0) b.Append(','); b.Append(CsvCell(cells[i])); }
                b.Append("\r\n");
            }
            return b.ToString();
        }
        public static string Html(ScanReport report)
        {
            var b = new StringBuilder("<!doctype html><html lang='en'><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'><title>Windows Security Posture Checker</title><style>body{font:15px system-ui;margin:40px;color:#17273e;background:#f3f6fa}h1{color:#163456}table{width:100%;border-collapse:collapse;background:white}td,th{padding:12px;border:1px solid #d4dce5;text-align:left;vertical-align:top;overflow-wrap:anywhere}th{background:#163456;color:white}.PASS{color:#167341}.WARNING{color:#ad263d}.UNKNOWN{color:#555}.REVIEW{color:#8b5700}footer{margin-top:24px}@media print{body{margin:12px}thead{display:table-header-group}tr{break-inside:avoid}}</style><h1>Windows Security Posture Checker</h1>");
            b.Append("<p>Machine: " + WebUtility.HtmlEncode(report.Machine) + " | UTC: " + WebUtility.HtmlEncode(report.CollectedUtc) + " | Elevated: " + report.Elevated + "</p>");
            b.Append("<p>Local evidence snapshot. PASS applies only to the named check. UNKNOWN is not a pass. This report is not a malware scan, vulnerability scan, or compliance certification.</p><table><thead><tr><th>Check</th><th>Status</th><th>Evidence</th><th>Guidance</th></tr></thead><tbody>");
            foreach (var f in report.Items)
                b.Append("<tr><td>" + WebUtility.HtmlEncode(f.Title) + "</td><td class='" + WebUtility.HtmlEncode(f.Status) + "'>" + WebUtility.HtmlEncode(f.Status) + "</td><td>" + WebUtility.HtmlEncode(f.Evidence) + "</td><td>" + WebUtility.HtmlEncode(f.Advice) + "</td></tr>");
            return b.Append("</tbody></table><footer>" + WebUtility.HtmlEncode(report.Credit) + "</footer></html>").ToString();
        }
        public static void Save(string path, string contents)
        {
            // Temporary file in the destination directory; preserve old file on write failure.
            string tmp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(tmp, contents, new UTF8Encoding(true));
                if (File.Exists(path)) File.Replace(tmp, path, null); else File.Move(tmp, path);
            }
            finally { if (File.Exists(tmp)) File.Delete(tmp); }
        }
    }
}
