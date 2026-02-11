using System;
using System.Linq;
using PickleCalLG.Meters;
using PickleCalLG.Meters.Sequences;
using Xunit;

namespace PickleCalLG.Tests
{
    public class MeasurementSequenceTests
    {
        // ---------- MeasurementStep ----------

        [Fact]
        public void MeasurementStep_Constructor_SetsProperties()
        {
            var step = new MeasurementStep("Gray 50", 50, MeterMeasurementMode.Display,
                TimeSpan.FromSeconds(2), true, "Gray 50%", MeasurementCategory.Grayscale, 128, 128, 128);

            Assert.Equal("Gray 50", step.Name);
            Assert.Equal(50.0, step.TargetIre);
            Assert.Equal(MeterMeasurementMode.Display, step.Mode);
            Assert.Equal(TimeSpan.FromSeconds(2), step.IntegrationTime);
            Assert.True(step.UseAveraging);
            Assert.Equal("Gray 50%", step.PatternDescription);
            Assert.Equal(MeasurementCategory.Grayscale, step.Category);
            Assert.Equal(128, step.PatternR);
            Assert.Equal(128, step.PatternG);
            Assert.Equal(128, step.PatternB);
        }

        [Fact]
        public void MeasurementStep_NullName_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new MeasurementStep(null!, 50, MeterMeasurementMode.Display, TimeSpan.Zero, false));
        }

        [Fact]
        public void MeasurementStep_IreOutOfRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MeasurementStep("Bad", -1, MeterMeasurementMode.Display, TimeSpan.Zero, false));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MeasurementStep("Bad", 101, MeterMeasurementMode.Display, TimeSpan.Zero, false));
        }

        [Fact]
        public void MeasurementStep_ToString_ContainsNameAndIre()
        {
            var step = new MeasurementStep("Gray 50", 50, MeterMeasurementMode.Display, TimeSpan.Zero, false);
            string str = step.ToString();
            Assert.Contains("Gray 50", str);
            Assert.Contains("50", str);
        }

        // ---------- MeasurementSequence ----------

        [Fact]
        public void MeasurementSequence_EmptySteps_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new MeasurementSequence("Empty", Array.Empty<MeasurementStep>()));
        }

        [Fact]
        public void MeasurementSequence_NullName_Throws()
        {
            var steps = new[] { new MeasurementStep("S1", 50, MeterMeasurementMode.Display, TimeSpan.Zero, false) };
            Assert.Throws<ArgumentNullException>(() =>
                new MeasurementSequence(null!, steps));
        }

        [Fact]
        public void MeasurementSequence_ToString_ContainsNameAndCount()
        {
            var steps = new[] { new MeasurementStep("S1", 50, MeterMeasurementMode.Display, TimeSpan.Zero, false) };
            var seq = new MeasurementSequence("Test", steps);
            Assert.Contains("Test", seq.ToString());
            Assert.Contains("1", seq.ToString());
        }

        // ---------- Grayscale sequences ----------

        [Fact]
        public void Grayscale10Point_Has10Steps()
        {
            var seq = MeasurementSequences.Grayscale10Point();
            Assert.Equal(10, seq.Steps.Count);
        }

        [Fact]
        public void Grayscale21Point_Has21Steps()
        {
            var seq = MeasurementSequences.Grayscale21Point();
            Assert.Equal(21, seq.Steps.Count);
        }

        [Fact]
        public void GrayscaleNPoint_StartsAt0_EndsAt100()
        {
            var seq = MeasurementSequences.GrayscaleNPoint(11);
            Assert.Equal(0.0, seq.Steps.First().TargetIre);
            Assert.Equal(100.0, seq.Steps.Last().TargetIre);
        }

        [Fact]
        public void GrayscaleNPoint_AllStepsAreGrayscaleCategory()
        {
            var seq = MeasurementSequences.GrayscaleNPoint(11);
            Assert.All(seq.Steps, s => Assert.Equal(MeasurementCategory.Grayscale, s.Category));
        }

        [Fact]
        public void GrayscaleNPoint_PatternRGBEqual()
        {
            var seq = MeasurementSequences.GrayscaleNPoint(5);
            Assert.All(seq.Steps, s =>
            {
                Assert.Equal(s.PatternR, s.PatternG);
                Assert.Equal(s.PatternG, s.PatternB);
            });
        }

        [Fact]
        public void GrayscaleNPoint_MinCount2()
        {
            var seq = MeasurementSequences.GrayscaleNPoint(1);
            Assert.Equal(2, seq.Steps.Count); // Clamped to 2
        }

        // ---------- Near-Black ----------

        [Fact]
        public void NearBlack_Has11Steps_0to10()
        {
            var seq = MeasurementSequences.NearBlack();
            Assert.Equal(11, seq.Steps.Count);
            Assert.Equal(0, seq.Steps.First().TargetIre);
            Assert.Equal(10, seq.Steps.Last().TargetIre);
        }

        [Fact]
        public void NearBlack_AllStepsAreNearBlackCategory()
        {
            var seq = MeasurementSequences.NearBlack();
            Assert.All(seq.Steps, s => Assert.Equal(MeasurementCategory.NearBlack, s.Category));
        }

        // ---------- Near-White ----------

        [Fact]
        public void NearWhite_Has11Steps_90to100()
        {
            var seq = MeasurementSequences.NearWhite();
            Assert.Equal(11, seq.Steps.Count);
            Assert.Equal(90, seq.Steps.First().TargetIre);
            Assert.Equal(100, seq.Steps.Last().TargetIre);
        }

        [Fact]
        public void NearWhite_AllStepsAreNearWhiteCategory()
        {
            var seq = MeasurementSequences.NearWhite();
            Assert.All(seq.Steps, s => Assert.Equal(MeasurementCategory.NearWhite, s.Category));
        }

        // ---------- Primary & Secondary Sweep ----------

        [Fact]
        public void PrimarySecondarySweep_ContainsPrimariesAndSecondaries()
        {
            var seq = MeasurementSequences.PrimarySecondarySweep();
            Assert.Contains(seq.Steps, s => s.Category == MeasurementCategory.Primary);
            Assert.Contains(seq.Steps, s => s.Category == MeasurementCategory.Secondary);
        }

        [Fact]
        public void PrimarySecondarySweep_ContainsAllSixColors()
        {
            var seq = MeasurementSequences.PrimarySecondarySweep();
            var names = seq.Steps.Select(s => s.Name.ToLower()).ToList();
            Assert.Contains(names, n => n.Contains("red"));
            Assert.Contains(names, n => n.Contains("green"));
            Assert.Contains(names, n => n.Contains("blue"));
            Assert.Contains(names, n => n.Contains("cyan"));
            Assert.Contains(names, n => n.Contains("magenta"));
            Assert.Contains(names, n => n.Contains("yellow"));
        }

        [Fact]
        public void PrimarySecondarySweep_RedStep_HasCorrectRGB()
        {
            var seq = MeasurementSequences.PrimarySecondarySweep();
            var red100 = seq.Steps.First(s => s.Name == "Red 100%");
            Assert.Equal(255, red100.PatternR);
            Assert.Equal(0, red100.PatternG);
            Assert.Equal(0, red100.PatternB);
        }

        [Fact]
        public void PrimarySecondarySweep_CyanStep_HasCorrectRGB()
        {
            var seq = MeasurementSequences.PrimarySecondarySweep();
            var cyan100 = seq.Steps.First(s => s.Name == "Cyan 100%");
            Assert.Equal(0, cyan100.PatternR);
            Assert.Equal(255, cyan100.PatternG);
            Assert.Equal(255, cyan100.PatternB);
        }

        // ---------- Saturation Sweeps ----------

        [Fact]
        public void SaturationSweep_DefaultSteps_Has5Levels()
        {
            var seq = MeasurementSequences.SaturationSweep("Red");
            Assert.Equal(5, seq.Steps.Count);
        }

        [Fact]
        public void SaturationSweep_AllStepsAreSaturationCategory()
        {
            var seq = MeasurementSequences.SaturationSweep("Green", 4);
            Assert.All(seq.Steps, s => Assert.Equal(MeasurementCategory.Saturation, s.Category));
        }

        [Fact]
        public void FullSaturationSweep_Has30Steps_6Colors5Levels()
        {
            var seq = MeasurementSequences.FullSaturationSweep(5);
            Assert.Equal(30, seq.Steps.Count);
        }

        [Fact]
        public void FullSaturationSweep_ContainsAllSixColors()
        {
            var seq = MeasurementSequences.FullSaturationSweep();
            var names = seq.Steps.Select(s => s.Name.ToLower()).ToList();
            Assert.Contains(names, n => n.Contains("red"));
            Assert.Contains(names, n => n.Contains("green"));
            Assert.Contains(names, n => n.Contains("blue"));
            Assert.Contains(names, n => n.Contains("cyan"));
            Assert.Contains(names, n => n.Contains("magenta"));
            Assert.Contains(names, n => n.Contains("yellow"));
        }

        // ---------- Contrast Ratio ----------

        [Fact]
        public void ContrastRatio_Has2Steps_WhiteAndBlack()
        {
            var seq = MeasurementSequences.ContrastRatio();
            Assert.Equal(2, seq.Steps.Count);
            Assert.Contains(seq.Steps, s => s.TargetIre == 100);
            Assert.Contains(seq.Steps, s => s.TargetIre == 0);
        }

        [Fact]
        public void ContrastRatio_WhiteStep_Is255()
        {
            var seq = MeasurementSequences.ContrastRatio();
            var white = seq.Steps.First(s => s.TargetIre == 100);
            Assert.Equal(255, white.PatternR);
            Assert.Equal(255, white.PatternG);
            Assert.Equal(255, white.PatternB);
        }

        [Fact]
        public void ContrastRatio_BlackStep_Is0()
        {
            var seq = MeasurementSequences.ContrastRatio();
            var black = seq.Steps.First(s => s.TargetIre == 0);
            Assert.Equal(0, black.PatternR);
            Assert.Equal(0, black.PatternG);
            Assert.Equal(0, black.PatternB);
        }

        // ---------- Measure Everything ----------

        [Fact]
        public void MeasureEverything_ContainsAllCategories()
        {
            var seq = MeasurementSequences.MeasureEverything(11);
            var categories = seq.Steps.Select(s => s.Category).Distinct().ToList();
            Assert.Contains(MeasurementCategory.Grayscale, categories);
            Assert.Contains(MeasurementCategory.NearBlack, categories);
            Assert.Contains(MeasurementCategory.NearWhite, categories);
            Assert.Contains(MeasurementCategory.Primary, categories);
            Assert.Contains(MeasurementCategory.Secondary, categories);
            Assert.Contains(MeasurementCategory.Saturation, categories);
            Assert.Contains(MeasurementCategory.ContrastRatio, categories);
        }

        [Fact]
        public void MeasureEverything_TotalStepsMatchComponents()
        {
            int n = 11;
            var everything = MeasurementSequences.MeasureEverything(n);
            // contrast(2) + grayscale(n) + nearBlack(11) + nearWhite(11) + primary/secondary(13) + fullSaturation(30)
            int expected = 2 + n + 11 + 11 + 13 + 30;
            Assert.Equal(expected, everything.Steps.Count);
        }

        // ---------- Averaging parameter ----------

        [Fact]
        public void Grayscale_UseAveraging_DefaultTrue()
        {
            var seq = MeasurementSequences.Grayscale10Point();
            Assert.All(seq.Steps, s => Assert.True(s.UseAveraging));
        }

        [Fact]
        public void Grayscale_UseAveragingFalse_AllFalse()
        {
            var seq = MeasurementSequences.Grayscale10Point(useAveraging: false);
            Assert.All(seq.Steps, s => Assert.False(s.UseAveraging));
        }

        [Fact]
        public void PrimarySecondarySweep_UseAveraging_DefaultFalse()
        {
            var seq = MeasurementSequences.PrimarySecondarySweep();
            Assert.All(seq.Steps, s => Assert.False(s.UseAveraging));
        }
    }
}
