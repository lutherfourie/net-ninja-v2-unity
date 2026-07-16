// Deterministic Hash01 + RngStream. Port of net-lab packages/core/rng.ts.
// Math.imul → unchecked 32-bit signed multiply.
namespace NetNinja.Core
{
    public static class Rng
    {
        static int Imul(int a, int b) => unchecked(a * b);

        /// <summary>Per-(seed, salt) roll in [0,1).</summary>
        public static double Hash01(int seed, int salt)
        {
            unchecked
            {
                uint h = (uint)(seed ^ Imul(salt, unchecked((int)0x9e3779b9u)));
                h = (uint)Imul((int)(h ^ (h >> 16)), unchecked((int)0x85ebca6bu));
                h = (uint)Imul((int)(h ^ (h >> 13)), unchecked((int)0xc2b2ae35u));
                h = h ^ (h >> 16);
                return h / 4294967296.0;
            }
        }
    }

    public sealed class RngStream
    {
        readonly int _seed;
        readonly int _channel;
        int _n;

        public RngStream(int seed, int channel)
        {
            _seed = seed;
            _channel = channel;
            _n = 0;
        }

        public double Next() => Rng.Hash01(_seed + _channel * 7919, _n++);

        public double Range(double min, double max) => min + (max - min) * Next();
    }
}
