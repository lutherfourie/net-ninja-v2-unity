// Deliberate violations for the Roslyn allowlist analyzer trip harness.
// Only compiled when building with -p:TripAnalyzer=true (excluded from default green builds).
// Expected diagnostics: NNDET001 (float) + NNDET002 (Math.Exp).
namespace NetNinja.Core.AnalyzerTrip
{
    public static class Trip
    {
        public static double Run()
        {
            float f = 1.0f;
            return System.Math.Exp(f);
        }
    }
}
