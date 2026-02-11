using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PickleCalLG.Meters;

namespace PickleCalLG.Export
{
    /// <summary>
    /// Exports CalibrationSession data to CSV format.
    /// </summary>
    public static class CsvExporter
    {
        /// <summary>Export all measurement points to a CSV file.</summary>
        public static void Export(CalibrationSession session, string filePath)
        {
            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            Export(session, writer);
        }

        /// <summary>Export all measurement points to a TextWriter.</summary>
        public static void Export(CalibrationSession session, TextWriter writer)
        {
            // Header
            writer.WriteLine("Name,Category,IRE,Luminance,x,y,X,Y,Z,CCT,ΔE76,ΔE94,ΔE2000,ΔE_ICtCp,Gamma,Ref_x,Ref_y,R_Error,G_Error,B_Error,Timestamp");

            foreach (var p in session.Points)
            {
                var fields = new[]
                {
                    Escape(p.Name),
                    p.Category.ToString(),
                    F(p.TargetIre, 1),
                    F(p.Luminance, 4),
                    F(p.MeasuredXy.X, 6),
                    F(p.MeasuredXy.Y, 6),
                    F(p.MeasuredXyz.X, 6),
                    F(p.MeasuredXyz.Y, 6),
                    F(p.MeasuredXyz.Z, 6),
                    F(p.CCT, 0),
                    F(p.DeltaE76, 4),
                    F(p.DeltaE94, 4),
                    F(p.DeltaE2000, 4),
                    F(p.DeltaEICtCp, 4),
                    p.EffectiveGamma.HasValue ? F(p.EffectiveGamma.Value, 3) : "",
                    F(p.ReferenceXy.X, 6),
                    F(p.ReferenceXy.Y, 6),
                    p.RgbError.HasValue ? F(p.RgbError.Value.R, 4) : "",
                    p.RgbError.HasValue ? F(p.RgbError.Value.G, 4) : "",
                    p.RgbError.HasValue ? F(p.RgbError.Value.B, 4) : "",
                    p.Timestamp.ToString("o")
                };
                writer.WriteLine(string.Join(",", fields));
            }
        }

        private static string F(double value, int decimals) =>
            value.ToString($"F{decimals}", CultureInfo.InvariantCulture);

        private static string Escape(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
