using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;

namespace Sabaz.SecurityPosture
{
    public sealed class Finding
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public string Evidence { get; set; }
        public string Advice { get; set; }
    }
    public sealed class ScanReport
    {
        public int SchemaVersion { get; set; }
        public string Machine { get; set; }
        public string CollectedUtc { get; set; }
        public bool Elevated { get; set; }
        public List<Finding> Items { get; set; }
        public string Credit { get { return "Coded by Cyber Security Engineer Mr Sabaz Ali Khan"; } }
    }
    public static class ReportCodec
    {
        public static JavaScriptSerializer Serializer()
        {
            return new JavaScriptSerializer { MaxJsonLength = 4 * 1024 * 1024, RecursionLimit = 32 };
        }
        public static ScanReport Parse(string json)
        {
            var report = Serializer().Deserialize<ScanReport>(json.Trim().TrimStart('\uFEFF'));
            if (report == null || report.SchemaVersion != 1 || report.Items == null || report.Items.Count == 0)
                throw new InvalidOperationException("Collector returned an invalid or empty report.");
            var states = new[] { "PASS", "WARNING", "REVIEW", "UNKNOWN", "INFO" };
            var ids = new HashSet<string>();
            foreach (var f in report.Items)
            {
                if (f == null || String.IsNullOrWhiteSpace(f.Id) || !ids.Add(f.Id) || !states.Contains(f.Status))
                    throw new InvalidOperationException("Collector returned invalid findings.");
            }
            return report;
        }
    }
}
