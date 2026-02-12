using System;
using PickleCalLG.ColorScience;
using Xunit;

namespace PickleCalLG.Tests
{
    public class CieXyTests
    {
        [Fact]
        public void ToXyz_D65_ReturnsNormalizedWhitePoint()
        {
            var d65 = Illuminants.D65;
            var xyz = d65.ToXyz(1.0);
            Assert.Equal(1.0, xyz.Y, 6);
            Assert.InRange(xyz.X, 0.9, 1.0); // X ≈ 0.9505
            Assert.InRange(xyz.Z, 1.0, 1.15); // Z ≈ 1.089
        }

        [Fact]
        public void ToXyz_ZeroY_ReturnsZero()
        {
            var xy = new CieXy(0.3, 0.0);
            var xyz = xy.ToXyz(1.0);
            Assert.Equal(0.0, xyz.X);
            Assert.Equal(0.0, xyz.Y);
            Assert.Equal(0.0, xyz.Z);
        }

        [Fact]
        public void DistanceTo_SamePoint_ReturnsZero()
        {
            var a = new CieXy(0.3127, 0.3290);
            Assert.Equal(0.0, a.DistanceTo(a), 10);
        }

        [Fact]
        public void DistanceTo_KnownPoints_ReturnsCorrectDistance()
        {
            var a = new CieXy(0.0, 0.0);
            var b = new CieXy(3.0, 4.0);
            Assert.Equal(5.0, a.DistanceTo(b), 10);
        }

        [Fact]
        public void Equality_SameValues_AreEqual()
        {
            var a = new CieXy(0.3127, 0.3290);
            var b = new CieXy(0.3127, 0.3290);
            Assert.Equal(a, b);
            Assert.True(a == b);
        }

        [Fact]
        public void Equality_DifferentValues_AreNotEqual()
        {
            var a = new CieXy(0.3127, 0.3290);
            var b = new CieXy(0.640, 0.330);
            Assert.NotEqual(a, b);
            Assert.True(a != b);
        }
    }

    public class CieXyzTests
    {
        [Fact]
        public void Luminance_ReturnsY()
        {
            var xyz = new CieXyz(0.5, 0.75, 0.3);
            Assert.Equal(0.75, xyz.Luminance);
        }

        [Fact]
        public void ToCieXy_KnownWhitePoint()
        {
            // D65 white point XYZ ≈ (0.9505, 1.0, 1.089)
            var xyz = Illuminants.D65.ToXyz(1.0);
            var xy = xyz.ToCieXy();
            Assert.Equal(Illuminants.D65.X, xy.X, 3);
            Assert.Equal(Illuminants.D65.Y, xy.Y, 3);
        }

        [Fact]
        public void ToCieXy_ZeroSum_ReturnsOrigin()
        {
            var xyz = new CieXyz(0, 0, 0);
            var xy = xyz.ToCieXy();
            Assert.Equal(0.0, xy.X);
            Assert.Equal(0.0, xy.Y);
        }

        [Fact]
        public void ToCieXyY_PreservesLuminance()
        {
            var xyz = new CieXyz(0.5, 0.75, 0.3);
            var xyY = xyz.ToCieXyY();
            Assert.Equal(0.75, xyY.Y, 10);
        }

        [Fact]
        public void ToLab_WhitePoint_GivesL100()
        {
            var wp = Illuminants.D65.ToXyz(1.0);
            var lab = wp.ToLab(wp);
            Assert.Equal(100.0, lab.L, 2);
            Assert.Equal(0.0, lab.A, 1);
            Assert.Equal(0.0, lab.B, 1);
        }

        [Fact]
        public void ToLab_Black_GivesL0()
        {
            var wp = Illuminants.D65.ToXyz(1.0);
            var black = new CieXyz(0, 0, 0);
            var lab = black.ToLab(wp);
            Assert.Equal(0.0, lab.L, 1);
        }

        [Fact]
        public void Operators_AddSubtractScale()
        {
            var a = new CieXyz(1, 2, 3);
            var b = new CieXyz(0.5, 0.5, 0.5);
            var sum = a + b;
            Assert.Equal(1.5, sum.X, 10);
            Assert.Equal(2.5, sum.Y, 10);
            Assert.Equal(3.5, sum.Z, 10);

            var diff = a - b;
            Assert.Equal(0.5, diff.X, 10);

            var scaled = a * 2;
            Assert.Equal(2.0, scaled.X, 10);
            Assert.Equal(4.0, scaled.Y, 10);
        }
    }

    public class CieXyYTests
    {
        [Fact]
        public void RoundTrip_XYZ_to_xyY_to_XYZ()
        {
            var original = new CieXyz(0.5, 0.75, 0.3);
            var xyY = original.ToCieXyY();
            var back = xyY.ToXyz();
            Assert.Equal(original.X, back.X, 6);
            Assert.Equal(original.Y, back.Y, 6);
            Assert.Equal(original.Z, back.Z, 6);
        }

        [Fact]
        public void ToCieXy_ReturnsOnlyChromaticity()
        {
            var xyY = new CieXyY(0.3127, 0.3290, 100.0);
            var xy = xyY.ToCieXy();
            Assert.Equal(0.3127, xy.X, 10);
            Assert.Equal(0.3290, xy.Y, 10);
        }
    }

    public class CieLabTests
    {
        [Fact]
        public void Chroma_IsLength()
        {
            var lab = new CieLab(50, 3.0, 4.0);
            Assert.Equal(5.0, lab.Chroma, 10);
        }

        [Fact]
        public void HueAngle_PureA_ReturnsZero()
        {
            var lab = new CieLab(50, 10.0, 0.0);
            Assert.Equal(0.0, lab.HueAngle, 6);
        }

        [Fact]
        public void HueAngle_PureB_Returns90()
        {
            var lab = new CieLab(50, 0.0, 10.0);
            Assert.Equal(90.0, lab.HueAngle, 6);
        }

        [Fact]
        public void HueAngle_NegativeA_BetweenRange()
        {
            var lab = new CieLab(50, -10.0, 0.0);
            Assert.Equal(180.0, lab.HueAngle, 6);
        }

        [Fact]
        public void ToLch_RoundTrip()
        {
            var lab = new CieLab(65.0, 25.0, -30.0);
            var lch = lab.ToLch();
            var back = lch.ToLab();
            Assert.Equal(lab.L, back.L, 6);
            Assert.Equal(lab.A, back.A, 6);
            Assert.Equal(lab.B, back.B, 6);
        }

        [Fact]
        public void RoundTrip_Lab_to_XYZ_to_Lab()
        {
            var wp = Illuminants.D65.ToXyz(1.0);
            var original = new CieLab(65.0, 25.0, -30.0);
            var xyz = original.ToXyz(wp);
            var back = xyz.ToLab(wp);
            Assert.Equal(original.L, back.L, 4);
            Assert.Equal(original.A, back.A, 4);
            Assert.Equal(original.B, back.B, 4);
        }
    }

    public class CieLchTests
    {
        [Fact]
        public void HueNormalization_NegativeWraps()
        {
            var lch = new CieLch(50, 30, -45);
            Assert.Equal(315.0, lch.H, 6);
        }

        [Fact]
        public void HueNormalization_Over360Wraps()
        {
            var lch = new CieLch(50, 30, 400);
            Assert.Equal(40.0, lch.H, 6);
        }

        [Fact]
        public void ToLab_ConvertsCorrectly()
        {
            var lch = new CieLch(50, 30, 60);
            var lab = lch.ToLab();
            Assert.Equal(50.0, lab.L, 6);
            Assert.Equal(15.0, lab.A, 4); // 30 * cos(60°) = 15
            Assert.Equal(30.0 * Math.Sin(60.0 * Math.PI / 180.0), lab.B, 4); // 30 * sin(60°) ≈ 25.98
        }
    }

    public class DeltaETests
    {
        [Fact]
        public void CIE76_IdenticalColors_ReturnsZero()
        {
            var a = new CieLab(50, 25, -30);
            Assert.Equal(0.0, DeltaE.CIE76(a, a), 10);
        }

        [Fact]
        public void CIE76_KnownDifference()
        {
            var a = new CieLab(50, 0, 0);
            var b = new CieLab(50, 3, 4);
            Assert.Equal(5.0, DeltaE.CIE76(a, b), 6);
        }

        [Fact]
        public void CIE94_IdenticalColors_ReturnsZero()
        {
            var a = new CieLab(50, 25, -30);
            Assert.Equal(0.0, DeltaE.CIE94(a, a), 10);
        }

        [Fact]
        public void CIE94_LessThanOrEqualTo_CIE76()
        {
            var a = new CieLab(50, 25, -30);
            var b = new CieLab(55, 30, -25);
            double de76 = DeltaE.CIE76(a, b);
            double de94 = DeltaE.CIE94(a, b);
            Assert.True(de94 <= de76, $"CIE94 ({de94:F4}) should be <= CIE76 ({de76:F4}) for chromatic colors");
        }

        [Fact]
        public void CIE2000_IdenticalColors_ReturnsZero()
        {
            var a = new CieLab(50, 25, -30);
            Assert.Equal(0.0, DeltaE.CIE2000(a, a), 10);
        }

        [Fact]
        public void CIE2000_KnownPair_WithinExpectedRange()
        {
            // Known test pair from Sharma 2005 paper (pair 1)
            var ref1 = new CieLab(50.0000, 2.6772, -79.7751);
            var sample1 = new CieLab(50.0000, 0.0000, -82.7485);
            double de = DeltaE.CIE2000(ref1, sample1);
            Assert.InRange(de, 2.0, 2.5); // Expected ≈ 2.0425
        }

        [Fact]
        public void CIE2000_AchromaticPair()
        {
            // Pure lightness difference (a=b=0)
            var a = new CieLab(50, 0, 0);
            var b = new CieLab(60, 0, 0);
            double de = DeltaE.CIE2000(a, b);
            Assert.True(de > 0);
            Assert.True(de < 10); // Lightness-only difference
        }

        [Fact]
        public void CIE2000_IsLessOrSimilarTo_CIE76_ForNeutralGrays()
        {
            var a = new CieLab(50, 0.5, -0.5);
            var b = new CieLab(55, 0.8, -0.3);
            double de76 = DeltaE.CIE76(a, b);
            double de2000 = DeltaE.CIE2000(a, b);
            // For near-neutral colors, DE2000 should be in a reasonable range relative to DE76
            Assert.True(de2000 > 0);
            Assert.True(de2000 < de76 * 3); // Loose bound
        }

        [Fact]
        public void ICtCp_IdenticalColors_ReturnsZero()
        {
            var a = new CieXyz(0.5, 0.5, 0.5);
            Assert.Equal(0.0, DeltaE.ICtCp(a, a), 6);
        }

        [Fact]
        public void ICtCp_DifferentColors_ReturnsPositive()
        {
            var a = new CieXyz(0.5, 0.5, 0.5);
            var b = new CieXyz(0.6, 0.5, 0.4);
            double de = DeltaE.ICtCp(a, b);
            Assert.True(de > 0);
        }
    }

    public class CctCalculatorTests
    {
        [Fact]
        public void McCamy_D65_ReturnsNear6504()
        {
            double cct = CctCalculator.McCamy(Illuminants.D65);
            Assert.InRange(cct, 6400, 6600);
        }

        [Fact]
        public void McCamy_D50_ReturnsNear5000()
        {
            double cct = CctCalculator.McCamy(Illuminants.D50);
            Assert.InRange(cct, 4900, 5200);
        }

        [Fact]
        public void McCamy_OverloadConsistency()
        {
            double fromXy = CctCalculator.McCamy(Illuminants.D65);
            var xyz = Illuminants.D65.ToXyz(1.0);
            double fromXyz = CctCalculator.McCamy(xyz);
            Assert.Equal(fromXy, fromXyz, 2);
        }

        [Fact]
        public void Duv_D65_NearZero()
        {
            double duv = CctCalculator.Duv(Illuminants.D65.X, Illuminants.D65.Y);
            Assert.InRange(Math.Abs(duv), 0, 0.02);
        }

        [Fact]
        public void PlanckianUv_ReturnsReasonableValues()
        {
            var (u, v) = CctCalculator.PlanckianUv(6504);
            Assert.InRange(u, 0.15, 0.35);
            Assert.InRange(v, 0.25, 0.40);
        }
    }

    public class EotfTests
    {
        [Fact]
        public void PowerLaw_Zero_ReturnsZero()
        {
            Assert.Equal(0.0, Eotf.PowerLaw(0, 2.2));
        }

        [Fact]
        public void PowerLaw_One_ReturnsOne()
        {
            Assert.Equal(1.0, Eotf.PowerLaw(1.0, 2.2), 10);
        }

        [Fact]
        public void PowerLaw_0_5_Gamma2_2()
        {
            double expected = Math.Pow(0.5, 2.2);
            Assert.Equal(expected, Eotf.PowerLaw(0.5, 2.2), 10);
        }

        [Fact]
        public void Srgb_Zero_ReturnsZero()
        {
            Assert.Equal(0.0, Eotf.Srgb(0.0), 10);
        }

        [Fact]
        public void Srgb_One_ReturnsOne()
        {
            Assert.Equal(1.0, Eotf.Srgb(1.0), 6);
        }

        [Fact]
        public void Srgb_RoundTrip_WithInverse()
        {
            for (double v = 0.0; v <= 1.0; v += 0.1)
            {
                double linear = Eotf.Srgb(v);
                double back = Eotf.SrgbInverse(linear);
                Assert.Equal(v, back, 6);
            }
        }

        [Fact]
        public void SrgbInverse_RoundTrip()
        {
            double signal = 0.5;
            double linear = Eotf.Srgb(signal);
            double roundTrip = Eotf.SrgbInverse(linear);
            Assert.Equal(signal, roundTrip, 10);
        }

        [Fact]
        public void Bt1886_Zero_ReturnsBlackLevel()
        {
            double val = Eotf.Bt1886(0.0, 100.0, 0.05);
            Assert.InRange(val, 0.0, 0.15);
        }

        [Fact]
        public void Bt1886_One_ReturnsPeakWhite()
        {
            double val = Eotf.Bt1886(1.0, 100.0, 0.0);
            Assert.Equal(100.0, val, 2);
        }

        [Fact]
        public void Pq_Zero_ReturnsZero()
        {
            Assert.Equal(0.0, Eotf.Pq(0.0));
        }

        [Fact]
        public void Pq_PqInverse_RoundTrip()
        {
            double signal = 0.5;
            double nits = Eotf.Pq(signal);
            double back = Eotf.PqInverse(nits);
            Assert.Equal(signal, back, 6);
        }

        [Fact]
        public void Pq_MaxSignal_Returns10000Nits()
        {
            double val = Eotf.Pq(1.0);
            Assert.Equal(10000.0, val, 0);
        }

        [Fact]
        public void Hlg_Zero_ReturnsZero()
        {
            Assert.Equal(0.0, Eotf.Hlg(0.0));
        }

        [Fact]
        public void Hlg_MonotonicallyIncreasing()
        {
            double prev = 0;
            for (double v = 0.1; v <= 1.0; v += 0.1)
            {
                double val = Eotf.Hlg(v);
                Assert.True(val > prev, $"HLG should increase: v={v}, val={val}, prev={prev}");
                prev = val;
            }
        }

        [Fact]
        public void LStar_Zero_ReturnsZero()
        {
            Assert.InRange(Eotf.LStar(0.0), -0.001, 0.001);
        }

        [Fact]
        public void LStar_One_ReturnsOne()
        {
            Assert.Equal(1.0, Eotf.LStar(1.0), 2);
        }

        [Fact]
        public void EffectiveGamma_HalfSignal_CorrectForGamma2_2()
        {
            double signal = 0.5;
            double peak = 100.0;
            double measuredLuminance = Math.Pow(signal, 2.2) * peak;
            double gamma = Eotf.EffectiveGamma(signal, measuredLuminance, peak);
            Assert.Equal(2.2, gamma, 4);
        }

        [Fact]
        public void EffectiveGamma_ZeroSignal_ReturnsNaN()
        {
            double gamma = Eotf.EffectiveGamma(0, 50, 100);
            Assert.True(double.IsNaN(gamma));
        }

        [Fact]
        public void EffectiveGamma_FullSignal_ReturnsNaN()
        {
            double gamma = Eotf.EffectiveGamma(1.0, 100, 100);
            Assert.True(double.IsNaN(gamma));
        }
    }

    public class ColorSpaceTests
    {
        [Fact]
        public void Rec709_WhitePoint_IsD65()
        {
            Assert.Equal(Illuminants.D65.X, ColorSpace.Rec709.WhitePoint.X, 10);
            Assert.Equal(Illuminants.D65.Y, ColorSpace.Rec709.WhitePoint.Y, 10);
        }

        [Fact]
        public void Rec709_RgbToXyz_White_HasChromaticityD65()
        {
            var xyz = ColorSpace.Rec709.RgbToXyz(1, 1, 1);
            var xy = xyz.ToCieXy();
            Assert.Equal(Illuminants.D65.X, xy.X, 2);
            Assert.Equal(Illuminants.D65.Y, xy.Y, 2);
        }

        [Fact]
        public void Rec709_RgbToXyz_Black_ReturnsZero()
        {
            var xyz = ColorSpace.Rec709.RgbToXyz(0, 0, 0);
            Assert.Equal(0.0, xyz.X, 10);
            Assert.Equal(0.0, xyz.Y, 10);
            Assert.Equal(0.0, xyz.Z, 10);
        }

        [Fact]
        public void RgbToXyz_XyzToRgb_RoundTrip()
        {
            var cs = ColorSpace.Rec709;
            double r = 0.5, g = 0.3, b = 0.8;
            var xyz = cs.RgbToXyz(r, g, b);
            var (rr, gg, bb) = cs.XyzToRgb(xyz);
            Assert.Equal(r, rr, 6);
            Assert.Equal(g, gg, 6);
            Assert.Equal(b, bb, 6);
        }

        [Fact]
        public void IsInsideGamut_White_IsInside()
        {
            var xyz = ColorSpace.Rec709.RgbToXyz(1, 1, 1);
            Assert.True(ColorSpace.Rec709.IsInsideGamut(xyz));
        }

        [Fact]
        public void IsInsideGamut_OutOfGamut_ReturnsFalse()
        {
            // A very saturated BT.2020 primary should be outside Rec709
            var xyz = ColorSpace.Bt2020.RgbToXyz(1, 0, 0);
            Assert.False(ColorSpace.Rec709.IsInsideGamut(xyz));
        }

        [Fact]
        public void GamutCoverage_SameSpace_Returns100()
        {
            double coverage = ColorSpace.Rec709.GamutCoverage(ColorSpace.Rec709);
            Assert.Equal(100.0, coverage, 2);
        }

        [Fact]
        public void GamutCoverage_Bt2020_LargerThanRec709()
        {
            double coverage = ColorSpace.Bt2020.GamutCoverage(ColorSpace.Rec709);
            Assert.True(coverage > 100.0, "BT.2020 should cover more than 100% of Rec.709");
        }

        [Fact]
        public void GamutCoverage_Rec709_SmallerThanBt2020()
        {
            double coverage = ColorSpace.Rec709.GamutCoverage(ColorSpace.Bt2020);
            Assert.True(coverage < 100.0, "Rec.709 should cover less than 100% of BT.2020");
        }

        [Fact]
        public void DciP3_Gamut_LargerThanRec709()
        {
            double coverage = ColorSpace.DciP3.GamutCoverage(ColorSpace.Rec709);
            Assert.True(coverage > 100.0, "DCI-P3 should cover more than 100% of Rec.709");
        }

        [Fact]
        public void AllColorSpaces_HaveNames()
        {
            Assert.False(string.IsNullOrEmpty(ColorSpace.Rec709.Name));
            Assert.False(string.IsNullOrEmpty(ColorSpace.DciP3.Name));
            Assert.False(string.IsNullOrEmpty(ColorSpace.Bt2020.Name));
            Assert.False(string.IsNullOrEmpty(ColorSpace.AdobeRgb.Name));
        }

        [Fact]
        public void AllColorSpaces_RoundTrip_RGB()
        {
            var spaces = new[] { ColorSpace.Rec709, ColorSpace.DciP3, ColorSpace.Bt2020, ColorSpace.AdobeRgb };
            foreach (var cs in spaces)
            {
                var xyz = cs.RgbToXyz(0.4, 0.6, 0.2);
                var (r, g, b) = cs.XyzToRgb(xyz);
                Assert.Equal(0.4, r, 4);
                Assert.Equal(0.6, g, 4);
                Assert.Equal(0.2, b, 4);
            }
        }
    }

    public class IlluminantsTests
    {
        [Fact]
        public void D65_KnownValues()
        {
            Assert.Equal(0.3127, Illuminants.D65.X, 4);
            Assert.Equal(0.3290, Illuminants.D65.Y, 4);
        }

        [Fact]
        public void E_IsEqualEnergy()
        {
            Assert.Equal(1.0 / 3.0, Illuminants.E.X, 10);
            Assert.Equal(1.0 / 3.0, Illuminants.E.Y, 10);
        }
    }
}
