// Portable FP helpers. Prefer these over raw System.Math for auditability.
// Allowlist: Sqrt Abs Min Max Floor Ceiling Truncate (+ provisional Log/Log2/Cos for plant).
namespace NetNinja.Core
{
    public static class Fp
    {
        public static double Sqrt(double x) => System.Math.Sqrt(x);
        public static double Abs(double x) => System.Math.Abs(x);
        public static double Min(double a, double b) => System.Math.Min(a, b);
        public static double Max(double a, double b) => System.Math.Max(a, b);
        public static double Floor(double x) => System.Math.Floor(x);
        public static double Ceiling(double x) => System.Math.Ceiling(x);
        public static double Truncate(double x) => System.Math.Truncate(x);

        /// <summary>JS Math.round for non-negative values (floor(x+0.5)). Scores/ms are non-neg.</summary>
        public static double RoundNonNeg(double x) => System.Math.Floor(x + 0.5);

        public static int RoundToIntNonNeg(double x) => (int)RoundNonNeg(x);

        /// <summary>JS Math.sign.</summary>
        public static double Sign(double x) => x < 0 ? -1 : x > 0 ? 1 : 0;

        public static double Hypot2(double x, double y) => Sqrt(x * x + y * y);

        // --- Provisional plant-only (ADR-0008). Required for golden bit-parity with net-lab. ---
        public static double Log(double x) => System.Math.Log(x);
        public static double Log2(double x) => System.Math.Log(x, 2);
        public static double Cos(double x) => System.Math.Cos(x);

        public const double Pi = 3.141592653589793;
    }
}
