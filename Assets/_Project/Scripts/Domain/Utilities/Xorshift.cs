namespace RedlineLegends.Utilities
{
    /// <summary>
    /// Tiny deterministic RNG for AI mistakes and grid shuffles. Deterministic per seed so a future
    /// networked or replayed race reproduces the same AI behaviour on every peer.
    /// </summary>
    public struct Xorshift
    {
        private uint _state;

        public Xorshift(int seed)
        {
            _state = (uint)seed;
            if (_state == 0) _state = 0x9E3779B9u;
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>Uniform float in [0,1).</summary>
        public float NextFloat() => (NextUInt() & 0xFFFFFF) / 16777216f;

        public float Range(float min, float max) => min + (max - min) * NextFloat();

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));
        }
    }
}
