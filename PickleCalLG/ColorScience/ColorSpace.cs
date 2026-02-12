using System;

namespace PickleCalLG.ColorScience
{
    /// <summary>
    /// Standard color space definitions with primaries, white point, and 3×3 conversion matrices.
    /// </summary>
    public sealed class ColorSpace
    {
        public string Name { get; }
        public CieXy Red { get; }
        public CieXy Green { get; }
        public CieXy Blue { get; }
        public CieXy WhitePoint { get; }

        /// <summary>Row-major 3×3 matrix: RGB → XYZ.</summary>
        public double[,] RgbToXyzMatrix { get; }

        /// <summary>Row-major 3×3 matrix: XYZ → RGB.</summary>
        public double[,] XyzToRgbMatrix { get; }

        private ColorSpace(string name, CieXy red, CieXy green, CieXy blue, CieXy whitePoint)
        {
            Name = name;
            Red = red;
            Green = green;
            Blue = blue;
            WhitePoint = whitePoint;
            RgbToXyzMatrix = ComputeRgbToXyz(red, green, blue, whitePoint);
            XyzToRgbMatrix = Invert3x3(RgbToXyzMatrix);
        }

        /// <summary>Convert linear RGB [0,1] to CIE XYZ.</summary>
        public CieXyz RgbToXyz(double r, double g, double b)
        {
            var m = RgbToXyzMatrix;
            return new CieXyz(
                m[0, 0] * r + m[0, 1] * g + m[0, 2] * b,
                m[1, 0] * r + m[1, 1] * g + m[1, 2] * b,
                m[2, 0] * r + m[2, 1] * g + m[2, 2] * b);
        }

        /// <summary>Convert CIE XYZ to linear RGB [0,1].</summary>
        public (double R, double G, double B) XyzToRgb(CieXyz xyz)
        {
            var m = XyzToRgbMatrix;
            return (
                m[0, 0] * xyz.X + m[0, 1] * xyz.Y + m[0, 2] * xyz.Z,
                m[1, 0] * xyz.X + m[1, 1] * xyz.Y + m[1, 2] * xyz.Z,
                m[2, 0] * xyz.X + m[2, 1] * xyz.Y + m[2, 2] * xyz.Z);
        }

        /// <summary>Check if an XYZ color is within the gamut of this color space.</summary>
        public bool IsInsideGamut(CieXyz xyz)
        {
            var (r, g, b) = XyzToRgb(xyz);
            return r >= -0.001 && r <= 1.001 &&
                   g >= -0.001 && g <= 1.001 &&
                   b >= -0.001 && b <= 1.001;
        }

        /// <summary>Gamut coverage: percentage of target gamut area covered by this space (CIE xy triangle area ratio).</summary>
        public double GamutCoverage(ColorSpace target)
        {
            double thisArea = TriangleArea(Red, Green, Blue);
            double targetArea = TriangleArea(target.Red, target.Green, target.Blue);
            if (targetArea <= 0) return 0;
            return (thisArea / targetArea) * 100.0;
        }

        private static double TriangleArea(CieXy a, CieXy b, CieXy c)
        {
            return 0.5 * Math.Abs(
                a.X * (b.Y - c.Y) +
                b.X * (c.Y - a.Y) +
                c.X * (a.Y - b.Y));
        }

        // ---------- Standard color spaces ----------

        public static readonly ColorSpace Rec709 = new("Rec.709 / sRGB",
            new CieXy(0.640, 0.330),
            new CieXy(0.300, 0.600),
            new CieXy(0.150, 0.060),
            Illuminants.D65);

        public static readonly ColorSpace DciP3 = new("DCI-P3",
            new CieXy(0.680, 0.320),
            new CieXy(0.265, 0.690),
            new CieXy(0.150, 0.060),
            Illuminants.D65);

        public static readonly ColorSpace Bt2020 = new("BT.2020",
            new CieXy(0.708, 0.292),
            new CieXy(0.170, 0.797),
            new CieXy(0.131, 0.046),
            Illuminants.D65);

        public static readonly ColorSpace AdobeRgb = new("Adobe RGB (1998)",
            new CieXy(0.640, 0.330),
            new CieXy(0.210, 0.710),
            new CieXy(0.150, 0.060),
            Illuminants.D65);

        // ---------- Matrix computation ----------

        private static double[,] ComputeRgbToXyz(CieXy r, CieXy g, CieXy b, CieXy w)
        {
            // Convert primaries to XYZ with Y=1
            var xr = r.ToXyz(1.0);
            var xg = g.ToXyz(1.0);
            var xb = b.ToXyz(1.0);
            var xw = w.ToXyz(1.0);

            // Build primary matrix
            double[,] P = {
                { xr.X, xg.X, xb.X },
                { xr.Y, xg.Y, xb.Y },
                { xr.Z, xg.Z, xb.Z }
            };

            // Solve for S = P^-1 * W
            var Pinv = Invert3x3(P);
            double Sr = Pinv[0, 0] * xw.X + Pinv[0, 1] * xw.Y + Pinv[0, 2] * xw.Z;
            double Sg = Pinv[1, 0] * xw.X + Pinv[1, 1] * xw.Y + Pinv[1, 2] * xw.Z;
            double Sb = Pinv[2, 0] * xw.X + Pinv[2, 1] * xw.Y + Pinv[2, 2] * xw.Z;

            return new double[,] {
                { Sr * P[0, 0], Sg * P[0, 1], Sb * P[0, 2] },
                { Sr * P[1, 0], Sg * P[1, 1], Sb * P[1, 2] },
                { Sr * P[2, 0], Sg * P[2, 1], Sb * P[2, 2] }
            };
        }

        private static double[,] Invert3x3(double[,] m)
        {
            double a = m[0, 0], b = m[0, 1], c = m[0, 2];
            double d = m[1, 0], e = m[1, 1], f = m[1, 2];
            double g = m[2, 0], h = m[2, 1], i = m[2, 2];

            double det = a * (e * i - f * h)
                       - b * (d * i - f * g)
                       + c * (d * h - e * g);

            if (Math.Abs(det) < 1e-15)
                throw new InvalidOperationException("Matrix is singular and cannot be inverted.");

            double invDet = 1.0 / det;
            return new double[,] {
                { (e * i - f * h) * invDet, (c * h - b * i) * invDet, (b * f - c * e) * invDet },
                { (f * g - d * i) * invDet, (a * i - c * g) * invDet, (c * d - a * f) * invDet },
                { (d * h - e * g) * invDet, (b * g - a * h) * invDet, (a * e - b * d) * invDet }
            };
        }

        public override string ToString() => Name;
    }
}
