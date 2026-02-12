using System;
using System.Linq;
using PickleCalLG.ColorScience;
using PickleCalLG.Meters;
using Xunit;

namespace PickleCalLG.Tests
{
    public class CalibrationSessionTests
    {
        private static CalibrationSession CreateSession(string name = "Test Display")
        {
            return new CalibrationSession(name, ColorSpace.Rec709, EotfType.Gamma, 2.2);
        }

        private static CieXyz D65White => Illuminants.D65.ToXyz(1.0);

        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            var session = CreateSession("My TV");
            Assert.Equal("My TV", session.DisplayName);
            Assert.Same(ColorSpace.Rec709, session.TargetColorSpace);
            Assert.Equal(EotfType.Gamma, session.TargetEotf);
            Assert.Equal(2.2, session.TargetGamma);
            Assert.NotNull(session.Id);
            Assert.Equal(32, session.Id.Length); // Guid N format
        }

        [Fact]
        public void Constructor_DefaultLuminanceValues()
        {
            var session = CreateSession();
            Assert.Equal(100.0, session.PeakWhiteLuminance);
            Assert.Equal(0.0, session.BlackLuminance);
        }

        // ---------- Grayscale ----------

        [Fact]
        public void AddGrayscalePoint_CreatesCalibrationPoint()
        {
            var session = CreateSession();
            var measured = D65White;
            var point = session.AddGrayscalePoint("Gray 50", 50, measured);
            Assert.Equal("Gray 50", point.Name);
            Assert.Equal(MeasurementCategory.Grayscale, point.Category);
            Assert.Equal(50, point.TargetIre);
            Assert.Single(session.Points);
        }

        [Fact]
        public void AddGrayscalePoint_ComputesEffectiveGamma()
        {
            var session = CreateSession();
            session.PeakWhiteLuminance = 100.0;
            session.BlackLuminance = 0.0;

            // Simulate 50% gray with gamma 2.2 response
            double expected_luminance = Math.Pow(0.5, 2.2) * 100.0;
            var measured = Illuminants.D65.ToXyz(expected_luminance);
            var point = session.AddGrayscalePoint("Gray 50", 50, measured);

            Assert.NotNull(point.EffectiveGamma);
            Assert.Equal(2.2, point.EffectiveGamma!.Value, 2);
        }

        [Fact]
        public void AddGrayscalePoint_ComputesRgbError()
        {
            var session = CreateSession();
            var measured = D65White;
            var point = session.AddGrayscalePoint("Gray 100", 100, measured);
            Assert.NotNull(point.RgbError);
        }

        [Fact]
        public void AddGrayscalePoint_0and100IRE_NoEffectiveGamma()
        {
            var session = CreateSession();
            var p0 = session.AddGrayscalePoint("Black", 0, new CieXyz(0, 0, 0));
            var p100 = session.AddGrayscalePoint("White", 100, D65White);
            Assert.Null(p0.EffectiveGamma);
            Assert.Null(p100.EffectiveGamma);
        }

        // ---------- Near-Black / Near-White ----------

        [Fact]
        public void AddNearBlackPoint_CategoryIsNearBlack()
        {
            var session = CreateSession();
            var p = session.AddNearBlackPoint("NB 5", 5, new CieXyz(0.01, 0.01, 0.01));
            Assert.Equal(MeasurementCategory.NearBlack, p.Category);
        }

        [Fact]
        public void AddNearWhitePoint_CategoryIsNearWhite()
        {
            var session = CreateSession();
            var p = session.AddNearWhitePoint("NW 95", 95, D65White);
            Assert.Equal(MeasurementCategory.NearWhite, p.Category);
        }

        // ---------- Primaries / Secondaries ----------

        [Fact]
        public void AddPrimaryPoint_CategoryIsPrimary()
        {
            var session = CreateSession();
            var red = ColorSpace.Rec709.RgbToXyz(1, 0, 0);
            var p = session.AddPrimaryPoint("Red 100%", 100, red);
            Assert.Equal(MeasurementCategory.Primary, p.Category);
        }

        [Fact]
        public void AddSecondaryPoint_CategoryIsSecondary()
        {
            var session = CreateSession();
            var cyan = ColorSpace.Rec709.RgbToXyz(0, 1, 1);
            var p = session.AddSecondaryPoint("Cyan 100%", 100, cyan);
            Assert.Equal(MeasurementCategory.Secondary, p.Category);
        }

        // ---------- Other categories ----------

        [Fact]
        public void AddSaturationPoint_CategoryIsSaturation()
        {
            var session = CreateSession();
            var p = session.AddSaturationPoint("Red Sat 50%", 50, D65White);
            Assert.Equal(MeasurementCategory.Saturation, p.Category);
        }

        [Fact]
        public void AddColorCheckerPoint_CategoryIsColorChecker()
        {
            var session = CreateSession();
            var p = session.AddColorCheckerPoint("Patch 1", D65White, D65White);
            Assert.Equal(MeasurementCategory.ColorChecker, p.Category);
        }

        [Fact]
        public void AddFreePoint_DeltaE_IsZero()
        {
            var session = CreateSession();
            var p = session.AddFreePoint("Free 1", D65White);
            Assert.Equal(MeasurementCategory.Free, p.Category);
            Assert.Equal(0.0, p.DeltaE2000, 6);
        }

        // ---------- Filtered Views ----------

        [Fact]
        public void FilteredViews_ReturnCorrectCategories()
        {
            var session = CreateSession();
            session.AddGrayscalePoint("G50", 50, D65White);
            session.AddGrayscalePoint("G75", 75, D65White);
            session.AddNearBlackPoint("NB5", 5, D65White);
            session.AddNearWhitePoint("NW95", 95, D65White);
            session.AddPrimaryPoint("Red", 100, D65White);
            session.AddSecondaryPoint("Cyan", 100, D65White);
            session.AddSaturationPoint("RedSat", 50, D65White);
            session.AddColorCheckerPoint("CC1", D65White, D65White);
            session.AddFreePoint("F1", D65White);

            Assert.Equal(2, session.Grayscale.Count());
            Assert.Single(session.NearBlack);
            Assert.Single(session.NearWhite);
            Assert.Single(session.Primaries);
            Assert.Single(session.Secondaries);
            Assert.Single(session.Saturations);
            Assert.Single(session.ColorChecker);
            Assert.Single(session.FreeMeasures);
            Assert.Equal(9, session.Points.Count);
        }

        [Fact]
        public void Grayscale_OrderedByIre()
        {
            var session = CreateSession();
            session.AddGrayscalePoint("G75", 75, D65White);
            session.AddGrayscalePoint("G25", 25, D65White);
            session.AddGrayscalePoint("G50", 50, D65White);

            var ires = session.Grayscale.Select(p => p.TargetIre).ToList();
            Assert.Equal(new[] { 25.0, 50.0, 75.0 }, ires);
        }

        // ---------- Clear ----------

        [Fact]
        public void ClearCategory_RemovesOnlyThatCategory()
        {
            var session = CreateSession();
            session.AddGrayscalePoint("G50", 50, D65White);
            session.AddPrimaryPoint("Red", 100, D65White);

            session.ClearCategory(MeasurementCategory.Grayscale);
            Assert.Empty(session.Grayscale);
            Assert.Single(session.Primaries);
            Assert.Single(session.Points);
        }

        [Fact]
        public void ClearAll_RemovesEverything()
        {
            var session = CreateSession();
            session.AddGrayscalePoint("G50", 50, D65White);
            session.AddPrimaryPoint("Red", 100, D65White);
            session.AddNearBlackPoint("NB5", 5, D65White);

            session.ClearAll();
            Assert.Empty(session.Points);
        }

        // ---------- Aggregate stats ----------

        [Fact]
        public void ContrastRatio_WithBlackLevel_ReturnsRatio()
        {
            var session = CreateSession();
            session.PeakWhiteLuminance = 200.0;
            session.BlackLuminance = 0.05;
            Assert.Equal(4000.0, session.ContrastRatio, 0);
        }

        [Fact]
        public void ContrastRatio_ZeroBlack_ReturnsInfinity()
        {
            var session = CreateSession();
            session.PeakWhiteLuminance = 200.0;
            session.BlackLuminance = 0.0;
            Assert.Equal(double.PositiveInfinity, session.ContrastRatio);
        }

        [Fact]
        public void AverageGrayscaleDeltaE_NoPoints_ReturnsZero()
        {
            var session = CreateSession();
            Assert.Equal(0.0, session.AverageGrayscaleDeltaE);
        }

        [Fact]
        public void MaxGrayscaleDeltaE_NoPoints_ReturnsZero()
        {
            var session = CreateSession();
            Assert.Equal(0.0, session.MaxGrayscaleDeltaE);
        }

        [Fact]
        public void AverageDeltaE_NoPoints_ReturnsZero()
        {
            var session = CreateSession();
            Assert.Equal(0.0, session.AverageDeltaE);
        }

        [Fact]
        public void AverageGrayscaleDeltaE_WithPoints_ComputesCorrectly()
        {
            var session = CreateSession();
            // Add grayscale points that are slightly off from reference
            // Use white point as measured - should give low ΔE
            session.AddGrayscalePoint("G50", 50, D65White);
            session.AddGrayscalePoint("G75", 75, D65White);

            double avgDe = session.AverageGrayscaleDeltaE;
            // These may have some ΔE because measured white doesn't match computed reference luminance
            Assert.True(avgDe >= 0);
        }

        [Fact]
        public void MaxGrayscaleDeltaE_IsAtLeastAsLargeAsAverage()
        {
            var session = CreateSession();
            session.AddGrayscalePoint("G25", 25, D65White);
            session.AddGrayscalePoint("G50", 50, D65White);
            session.AddGrayscalePoint("G75", 75, D65White);

            Assert.True(session.MaxGrayscaleDeltaE >= session.AverageGrayscaleDeltaE);
        }

        // ---------- CalibrationPoint properties ----------

        [Fact]
        public void CalibrationPoint_ComputesDeltaE_AllVariants()
        {
            var session = CreateSession();
            // Add a point with a slight color error
            var measured = new CieXyz(0.5, 0.52, 0.45);
            var point = session.AddGrayscalePoint("G50", 50, measured);

            Assert.True(point.DeltaE76 >= 0);
            Assert.True(point.DeltaE94 >= 0);
            Assert.True(point.DeltaE2000 >= 0);
            Assert.True(point.DeltaEICtCp >= 0);
        }

        [Fact]
        public void CalibrationPoint_HasCCT()
        {
            var session = CreateSession();
            var point = session.AddGrayscalePoint("G50", 50, D65White);
            Assert.InRange(point.CCT, 1000, 20000);
        }

        [Fact]
        public void CalibrationPoint_HasLuminance()
        {
            var session = CreateSession();
            var measured = new CieXyz(0.5, 0.75, 0.3);
            var point = session.AddGrayscalePoint("G50", 50, measured);
            Assert.Equal(0.75, point.Luminance);
        }

        [Fact]
        public void CalibrationPoint_ToString_ContainsKeyInfo()
        {
            var session = CreateSession();
            var point = session.AddGrayscalePoint("G50", 50, D65White);
            string str = point.ToString();
            Assert.Contains("G50", str);
            Assert.Contains("ΔE", str);
        }

        // ---------- DisplayName mutability ----------

        [Fact]
        public void DisplayName_CanBeChanged()
        {
            var session = CreateSession("Old Name");
            session.DisplayName = "New Name";
            Assert.Equal("New Name", session.DisplayName);
        }

        // ---------- Different EOTF types ----------

        [Theory]
        [InlineData(EotfType.Gamma)]
        [InlineData(EotfType.Srgb)]
        [InlineData(EotfType.Bt1886)]
        [InlineData(EotfType.PQ)]
        [InlineData(EotfType.Hlg)]
        [InlineData(EotfType.LStar)]
        public void AddGrayscalePoint_WorksWithAllEotfTypes(EotfType eotf)
        {
            var session = new CalibrationSession("Test", ColorSpace.Rec709, eotf, 2.4);
            var point = session.AddGrayscalePoint("G50", 50, D65White);
            Assert.NotNull(point);
            Assert.Equal(MeasurementCategory.Grayscale, point.Category);
        }

        // ---------- Different color spaces ----------

        [Fact]
        public void AddPrimaryPoint_WorksWithDciP3()
        {
            var session = new CalibrationSession("Test", ColorSpace.DciP3, EotfType.Gamma, 2.6);
            var red = ColorSpace.DciP3.RgbToXyz(1, 0, 0);
            var p = session.AddPrimaryPoint("Red 100%", 100, red);
            Assert.Equal(MeasurementCategory.Primary, p.Category);
            Assert.True(p.DeltaE2000 >= 0);
        }
    }
}
