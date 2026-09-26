using System;
using System.Collections.Generic;
using Sabaz.SecurityPosture;
internal static class Tests
{
    private static void Assert(bool test, string description) { if (!test) throw new Exception(description); }
    private static void Main()
    {
        var report = new ScanReport { SchemaVersion=1, Machine="TEST-PC", CollectedUtc="2026-09-26T00:00:00Z", Elevated=false, Items=new List<Finding> {
            new Finding {Id="x",Title="<script>alert(1)</script>",Status="UNKNOWN",Evidence="=1+1",Advice="Review & verify"}
        }};
        string json=ReportCodec.Serializer().Serialize(report);
        Assert(ReportCodec.Parse(json).Items[0].Status=="UNKNOWN","Unknown must remain unknown.");
        string html=Export.Html(report);
        Assert(!html.Contains("<script>"),"HTML must escape evidence.");
        Assert(html.Contains("&lt;script&gt;"),"Encoded label missing.");
        Assert(Export.Csv(report).Contains("\"'=1+1\""),"Spreadsheet formula must be neutralized.");
        Assert(Export.CsvCell("a\"b")=="\"a\"\"b\"","CSV quotes must be escaped.");
        foreach(string bad in new[]{"{}","{\"SchemaVersion\":1,\"Items\":[]}",json.Replace("UNKNOWN","SECURE")}) {
            bool rejected=false; try {ReportCodec.Parse(bad);} catch(Exception){rejected=true;}
            Assert(rejected,"Invalid schema accepted.");
        }
        report.Items.Add(report.Items[0]);
        bool duplicate=false; try {ReportCodec.Parse(ReportCodec.Serializer().Serialize(report));} catch(Exception){duplicate=true;}
        Assert(duplicate,"Duplicate IDs accepted.");
        Console.WriteLine("PASS: report validation, unknown preservation, HTML escaping, CSV formula/quote protection.");
    }
}
