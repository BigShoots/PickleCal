using System;

namespace PickleCalLG.ColorScience
{
    /// <summary>
    /// Correlated Color Temperature (CCT) calculation using McCamy's approximation
    /// and Ohno 2014 method.
    /// </summary>
    public static class CctCalculator
    {
        /// <summary>
        /// McCamy's approximation: CCT from CIE xy chromaticity.
        /// Accurate to ~2 K for 2000–12500 K range.
        /// </summary>
        public static double McCamy(double x, double y)
        {
            double n = (x - 0.3320) / (0.1858 - y);
            return 449.0 * n * n * n + 3525.0 * n * n + 6823.3 * n + 5520.33;
        }

        /// <summary>McCamy from CieXy.</summary>
        public static double McCamy(CieXy xy) => McCamy(xy.X, xy.Y);

        /// <summary>McCamy from CIE XYZ.</summary>
        public static double McCamy(CieXyz xyz)
        {
            var xy = xyz.ToCieXy();
            return McCamy(xy.X, xy.Y);
        }

        /// <summary>
        /// Compute the Duv (distance from the Planckian locus) using Robertson's method.
        /// Negative values mean the chromaticity is below the locus (greenish),
        /// positive means above (pinkish).
        /// </summary>
        public static double Duv(double x, double y)
        {
            // CIE 1960 UCS
            double u = 4.0 * x / (-2.0 * x + 12.0 * y + 3.0);
            double v = 6.0 * y / (-2.0 * x + 12.0 * y + 3.0);

            // Planckian locus point for estimated CCT
            double cct = McCamy(x, y);
            var (up, vp) = PlanckianUv(cct);

            double du = u - up;
            double dv = v - vp;
            double dist = Math.Sqrt(du * du + dv * dv);

            // Sign: positive if above locus (higher v → pinkish)
            return v >= vp ? dist : -dist;
        }

        /// <summary>Compute CIE 1960 UCS (u,v) of the Planckian locus at given CCT.</summary>
        public static (double u, double v) PlanckianUv(double cct)
        {
            // Approximation from Robertson (1968)
            double t = cct;
            double t2 = t * t;
            double t3 = t2 * t;

            double u, v;
            if (t <= 4000)
            {
                u = (0.860117757 + 1.54118254e-4 * t + 1.28641212e-7 * t2) /
                    (1.0 + 8.42420235e-4 * t + 7.08145163e-7 * t2);
                v = (0.317398726 + 4.22806245e-5 * t + 4.20481691e-8 * t2) /
                    (1.0 - 2.89741816e-5 * t + 1.61456053e-7 * t2);
            }
            else
            {
                u = (0.860117757 + 1.54118254e-4 * t + 1.28641212e-7 * t2) /
                    (1.0 + 8.42420235e-4 * t + 7.08145163e-7 * t2);
                v = (0.317398726 + 4.22806245e-5 * t + 4.20481691e-8 * t2) /
                    (1.0 - 2.89741816e-5 * t + 1.61456053e-7 * t2);
            }

            return (u, v);
        }
    }
}
