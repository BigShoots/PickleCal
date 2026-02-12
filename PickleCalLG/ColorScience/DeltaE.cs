using System;

namespace PickleCalLG.ColorScience
{
    /// <summary>
    /// Color difference (ΔE) calculation formulas.
    /// All methods take two CIE L*a*b* values and return ΔE.
    /// </summary>
    public static class DeltaE
    {
        /// <summary>CIE76 simple Euclidean distance in Lab.</summary>
        public static double CIE76(CieLab reference, CieLab sample)
        {
            double dL = reference.L - sample.L;
            double da = reference.A - sample.A;
            double db = reference.B - sample.B;
            return Math.Sqrt(dL * dL + da * da + db * db);
        }

        /// <summary>CIE94 weighted distance (graphic arts defaults).</summary>
        public static double CIE94(CieLab reference, CieLab sample,
            double kL = 1.0, double kC = 1.0, double kH = 1.0)
        {
            double dL = reference.L - sample.L;
            double c1 = reference.Chroma;
            double c2 = sample.Chroma;
            double dC = c1 - c2;

            double da = reference.A - sample.A;
            double db = reference.B - sample.B;
            double dH2 = da * da + db * db - dC * dC;
            double dH = dH2 > 0 ? Math.Sqrt(dH2) : 0;

            double sL = 1.0;
            double sC = 1.0 + 0.045 * c1;
            double sH = 1.0 + 0.015 * c1;

            double term1 = dL / (kL * sL);
            double term2 = dC / (kC * sC);
            double term3 = dH / (kH * sH);

            return Math.Sqrt(term1 * term1 + term2 * term2 + term3 * term3);
        }

        /// <summary>CIEDE2000 — the most perceptually uniform formula.</summary>
        public static double CIE2000(CieLab reference, CieLab sample,
            double kL = 1.0, double kC = 1.0, double kH = 1.0)
        {
            double L1 = reference.L, a1 = reference.A, b1 = reference.B;
            double L2 = sample.L, a2 = sample.A, b2 = sample.B;

            double Cab1 = Math.Sqrt(a1 * a1 + b1 * b1);
            double Cab2 = Math.Sqrt(a2 * a2 + b2 * b2);
            double CabMean = (Cab1 + Cab2) / 2.0;
            double CabMean7 = Math.Pow(CabMean, 7);
            double G = 0.5 * (1.0 - Math.Sqrt(CabMean7 / (CabMean7 + Math.Pow(25.0, 7))));

            double a1p = a1 * (1.0 + G);
            double a2p = a2 * (1.0 + G);

            double Cp1 = Math.Sqrt(a1p * a1p + b1 * b1);
            double Cp2 = Math.Sqrt(a2p * a2p + b2 * b2);

            double h1p = HueAngle(b1, a1p);
            double h2p = HueAngle(b2, a2p);

            double dLp = L2 - L1;
            double dCp = Cp2 - Cp1;

            double dhp;
            if (Cp1 * Cp2 < 1e-10)
                dhp = 0;
            else if (Math.Abs(h2p - h1p) <= 180.0)
                dhp = h2p - h1p;
            else if (h2p - h1p > 180.0)
                dhp = h2p - h1p - 360.0;
            else
                dhp = h2p - h1p + 360.0;

            double dHp = 2.0 * Math.Sqrt(Cp1 * Cp2) * Math.Sin(Deg2Rad(dhp / 2.0));

            double Lpm = (L1 + L2) / 2.0;
            double Cpm = (Cp1 + Cp2) / 2.0;

            double hpm;
            if (Cp1 * Cp2 < 1e-10)
                hpm = h1p + h2p;
            else if (Math.Abs(h1p - h2p) <= 180.0)
                hpm = (h1p + h2p) / 2.0;
            else if (h1p + h2p < 360.0)
                hpm = (h1p + h2p + 360.0) / 2.0;
            else
                hpm = (h1p + h2p - 360.0) / 2.0;

            double T = 1.0
                - 0.17 * Math.Cos(Deg2Rad(hpm - 30.0))
                + 0.24 * Math.Cos(Deg2Rad(2.0 * hpm))
                + 0.32 * Math.Cos(Deg2Rad(3.0 * hpm + 6.0))
                - 0.20 * Math.Cos(Deg2Rad(4.0 * hpm - 63.0));

            double Lpm50sq = (Lpm - 50.0) * (Lpm - 50.0);
            double SL = 1.0 + 0.015 * Lpm50sq / Math.Sqrt(20.0 + Lpm50sq);
            double SC = 1.0 + 0.045 * Cpm;
            double SH = 1.0 + 0.015 * Cpm * T;

            double Cpm7 = Math.Pow(Cpm, 7);
            double RC = 2.0 * Math.Sqrt(Cpm7 / (Cpm7 + Math.Pow(25.0, 7)));

            double dTheta = 30.0 * Math.Exp(-((hpm - 275.0) / 25.0) * ((hpm - 275.0) / 25.0));
            double RT = -Math.Sin(Deg2Rad(2.0 * dTheta)) * RC;

            double term1 = dLp / (kL * SL);
            double term2 = dCp / (kC * SC);
            double term3 = dHp / (kH * SH);

            return Math.Sqrt(
                term1 * term1 +
                term2 * term2 +
                term3 * term3 +
                RT * term2 * term3);
        }

        /// <summary>ICtCp-based delta E (Pytlarz formula) for HDR content.</summary>
        public static double ICtCp(CieXyz reference, CieXyz sample)
        {
            // Simplified version: convert XYZ→ICtCp using PQ transfer
            var (i1, ct1, cp1) = XyzToICtCp(reference);
            var (i2, ct2, cp2) = XyzToICtCp(sample);

            double di = i1 - i2;
            double dct = ct1 - ct2;
            double dcp = cp1 - cp2;

            return 720.0 * Math.Sqrt(di * di + dct * dct + dcp * dcp);
        }

        private static (double I, double Ct, double Cp) XyzToICtCp(CieXyz xyz)
        {
            // XYZ → LMS (Hunt-Pointer-Estevez)
            double l = 0.4002 * xyz.X + 0.7076 * xyz.Y - 0.0808 * xyz.Z;
            double m = -0.2263 * xyz.X + 1.1653 * xyz.Y + 0.0457 * xyz.Z;
            double s = 0.0000 * xyz.X + 0.0000 * xyz.Y + 0.9182 * xyz.Z;

            // PQ transfer
            double lp = PQ(l / 10000.0);
            double mp = PQ(m / 10000.0);
            double sp = PQ(s / 10000.0);

            // ICtCp
            double I = 0.5 * lp + 0.5 * mp;
            double Ct = 1.6137 * lp - 3.3234 * mp + 1.7097 * sp;
            double Cp = 4.3781 * lp - 4.2455 * mp - 0.1326 * sp;

            return (I, Ct, Cp);
        }

        private static double PQ(double v)
        {
            const double m1 = 0.1593017578125;
            const double m2 = 78.84375;
            const double c1 = 0.8359375;
            const double c2 = 18.8515625;
            const double c3 = 18.6875;

            if (v < 0) v = 0;
            double vm1 = Math.Pow(v, m1);
            return Math.Pow((c1 + c2 * vm1) / (1.0 + c3 * vm1), m2);
        }

        private static double HueAngle(double b, double a)
        {
            double h = Math.Atan2(b, a) * (180.0 / Math.PI);
            if (h < 0) h += 360.0;
            return h;
        }

        private static double Deg2Rad(double deg) => deg * (Math.PI / 180.0);
    }
}
