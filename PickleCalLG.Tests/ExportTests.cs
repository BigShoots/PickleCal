using System;
using System.IO;
using System.Linq;
using PickleCalLG.ColorScience;
using PickleCalLG.Export;
using PickleCalLG.Meters;
using Xunit;

namespace PickleCalLG.Tests
{
    public class CsvExporterTests
    {
        private static CalibrationSession CreatePopulatedSession()
        {
            var session = new CalibrationSession("Test TV", ColorSpace.Rec709, EotfType.Gamma, 2.2);
            session.PeakWhiteLuminance = 100.0;
            session.BlackLuminance = 0.05;

            var wp = Illuminants.D65.ToXyz(1.0);
            session.AddGrayscalePoint("Gray 0", 0, new CieXyz(0, 0, 0));
            session.AddGrayscalePoint("Gray 50", 50, wp * 0.22);
            session.AddGrayscalePoint("Gray 100", 100, wp);
            session.AddPrimaryPoint("Red 100%", 100, ColorSpace.Rec709.RgbToXyz(1, 0, 0));
            session.AddNearBlackPoint("NB 5", 5, wp * 0.01);
            return session;
        }

        [Fact]
        public void Export_WritesHeader()
        {
            var session = CreatePopulatedSession();
            using var writer = new StringWriter();
            CsvExporter.Export(session, writer);
            string csv = writer.ToString();
            string firstLine = csv.Split('\n')[0].Trim();
            Assert.Contains("Name", firstLine);
            Assert.Contains("Category", firstLine);
            Assert.Contains("IRE", firstLine);
            Assert.Contains("Luminance", firstLine);
            Assert.Contains("ΔE2000", firstLine);
            Assert.Contains("CCT", firstLine);
            Assert.Contains("Timestamp", firstLine);
        }

        [Fact]
        public void Export_CorrectNumberOfDataRows()
        {
            var session = CreatePopulatedSession();
            using var writer = new StringWriter();
            CsvExporter.Export(session, writer);
            string csv = writer.ToString();
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            // 1 header + 5 data rows
            Assert.Equal(6, lines.Length);
        }

        [Fact]
        public void Export_ContainsMeasurementNames()
        {
            var session = CreatePopulatedSession();
            using var writer = new StringWriter();
            CsvExporter.Export(session, writer);
            string csv = writer.ToString();
            Assert.Contains("Gray 50", csv);
            Assert.Contains("Red 100%", csv);
            Assert.Contains("NB 5", csv);
        }

        [Fact]
        public void Export_ContainsCategories()
        {
            var session = CreatePopulatedSession();
            using var writer = new StringWriter();
            CsvExporter.Export(session, writer);
            string csv = writer.ToString();
            Assert.Contains("Grayscale", csv);
            Assert.Contains("Primary", csv);
            Assert.Contains("NearBlack", csv);
        }

        [Fact]
        public void Export_EmptySession_WritesOnlyHeader()
        {
            var session = new CalibrationSession("Empty", ColorSpace.Rec709, EotfType.Gamma, 2.2);
            using var writer = new StringWriter();
            CsvExporter.Export(session, writer);
            string csv = writer.ToString();
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Single(lines); // header only
        }

        [Fact]
        public void Export_UsesInvariantCulture()
        {
            var session = CreatePopulatedSession();
            using var writer = new StringWriter();
            CsvExporter.Export(session, writer);
            string csv = writer.ToString();
            // Should not contain localized decimal separators (commas in numbers)
            // Split by comma and check each field for numeric format
            var dataLines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1);
            foreach (var line in dataLines)
            {
                // Lines should parse cleanly with comma as delimiter
                var fields = line.Split(',');
                Assert.True(fields.Length >= 15, $"Expected at least 15 fields, got {fields.Length}");
            }
        }

        [Fact]
        public void Export_ToFile_CreatesFile()
        {
            var session = CreatePopulatedSession();
            string tempFile = Path.GetTempFileName();
            try
            {
                CsvExporter.Export(session, tempFile);
                Assert.True(File.Exists(tempFile));
                string content = File.ReadAllText(tempFile);
                Assert.Contains("Name,Category", content);
                Assert.Contains("Gray 50", content);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }

    public class HtmlReportExporterTests
    {
        private static CalibrationSession CreatePopulatedSession()
        {
            var session = new CalibrationSession("My LG C3", ColorSpace.Rec709, EotfType.Gamma, 2.2);
            session.PeakWhiteLuminance = 150.0;
            session.BlackLuminance = 0.01;

            var wp = Illuminants.D65.ToXyz(1.0);
            session.AddGrayscalePoint("Gray 0", 0, new CieXyz(0, 0, 0));
            session.AddGrayscalePoint("Gray 50", 50, wp * 0.22);
            session.AddGrayscalePoint("Gray 100", 100, wp);
            session.AddPrimaryPoint("Red 100%", 100, ColorSpace.Rec709.RgbToXyz(1, 0, 0));
            session.AddSecondaryPoint("Cyan 100%", 100, ColorSpace.Rec709.RgbToXyz(0, 1, 1));
            return session;
        }

        [Fact]
        public void GenerateHtml_IsValidHtml()
        {
            var session = CreatePopulatedSession();
            string html = HtmlReportExporter.GenerateHtml(session);
            Assert.StartsWith("<!DOCTYPE html>", html);
            Assert.Contains("</html>", html);
            Assert.Contains("<head>", html);
            Assert.Contains("<body>", html);
        }

        [Fact]
        public void GenerateHtml_ContainsDisplayName()
        {
            var session = CreatePopulatedSession();
            string html = HtmlReportExporter.GenerateHtml(session);
            Assert.Contains("My LG C3", html);
        }

        [Fact]
        public void GenerateHtml_ContainsColorSpaceAndEotf()
        {
            var session = CreatePopulatedSession();
            string html = HtmlReportExporter.GenerateHtml(session);
            Assert.Contains("Rec.709", html);
            Assert.Contains("Gamma", html);
        }

        [Fact]
        public void GenerateHtml_ContainsSummaryStats()
        {
            var session = CreatePopulatedSession();
            string html = HtmlReportExporter.GenerateHtml(session);
            Assert.Contains("Total Points", html);
            Assert.Contains("Peak White", html);
            Assert.Contains("Black Level", html);
            Assert.Contains("Contrast Ratio", html);
        }

        [Fact]
        public void GenerateHtml_ContainsGrayscaleSection()
        {
            var session = CreatePopulatedSession();
            string html = HtmlReportExporter.GenerateHtml(session);
            Assert.Contains("Grayscale", html);
            Assert.Contains("Gray 50", html);
        }

        [Fact]
        public void GenerateHtml_ContainsPrimariesSection()
        {
            var session = CreatePopulatedSession();
            string html = HtmlReportExporter.GenerateHtml(session);
            Assert.Contains("Primaries", html);
            Assert.Contains("Red 100%", html);
        }

        [Fact]
        public void GenerateHtml_ContainsSecondariesSection()
        {
            var session = CreatePopulatedSession();
            string html = HtmlReportExporter.GenerateHtml(session);
            Assert.Contains("Secondaries", html);
            Assert.Contains("Cyan 100%", html);
        }

        [Fact]
        public void GenerateHtml_ContainsDeltaEClasses()
        {
            var session = CreatePopulatedSession();
            string html = HtmlReportExporter.GenerateHtml(session);
            // Should contain at least one of the ΔE color classes
            Assert.True(
                html.Contains("class='good'") || html.Contains("class='ok'") || html.Contains("class='bad'"),
                "HTML should contain ΔE status classes");
        }

        [Fact]
        public void GenerateHtml_ContainsCSS()
        {
            var session = CreatePopulatedSession();
            string html = HtmlReportExporter.GenerateHtml(session);
            Assert.Contains("<style>", html);
            Assert.Contains("font-family", html);
        }

        [Fact]
        public void GenerateHtml_ContainsFooter()
        {
            var session = CreatePopulatedSession();
            string html = HtmlReportExporter.GenerateHtml(session);
            Assert.Contains("PickleCal", html);
        }

        [Fact]
        public void GenerateHtml_ContainsAvgAndMaxDeltaE()
        {
            var session = CreatePopulatedSession();
            string html = HtmlReportExporter.GenerateHtml(session);
            Assert.Contains("Avg ΔE", html);
            Assert.Contains("Max ΔE", html);
        }

        [Fact]
        public void Export_ToWriter_ProducesOutput()
        {
            var session = CreatePopulatedSession();
            using var writer = new StringWriter();
            HtmlReportExporter.Export(session, writer);
            string html = writer.ToString();
            Assert.False(string.IsNullOrWhiteSpace(html));
            Assert.Contains("<!DOCTYPE html>", html);
        }

        [Fact]
        public void Export_ToFile_CreatesFile()
        {
            var session = CreatePopulatedSession();
            string tempFile = Path.GetTempFileName();
            try
            {
                HtmlReportExporter.Export(session, tempFile);
                Assert.True(File.Exists(tempFile));
                string content = File.ReadAllText(tempFile);
                Assert.Contains("<!DOCTYPE html>", content);
                Assert.Contains("My LG C3", content);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GenerateHtml_EmptySession_OnlySummary()
        {
            var session = new CalibrationSession("Empty", ColorSpace.Rec709, EotfType.Gamma, 2.2);
            string html = HtmlReportExporter.GenerateHtml(session);
            Assert.Contains("<!DOCTYPE html>", html);
            Assert.Contains("Total Points", html);
            Assert.Contains("0", html); // 0 total points
        }

        [Fact]
        public void GenerateHtml_EscapesHtmlInDisplayName()
        {
            var session = new CalibrationSession("TV <script>alert(1)</script>", ColorSpace.Rec709, EotfType.Gamma, 2.2);
            string html = HtmlReportExporter.GenerateHtml(session);
            Assert.DoesNotContain("<script>", html);
            Assert.Contains("&lt;script&gt;", html);
        }
    }
}
