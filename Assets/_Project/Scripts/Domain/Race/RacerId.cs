using System;

namespace RedlineLegends.Race
{
    /// <summary>
    /// Stable identity of a race participant for the lifetime of a session.
    /// Local player, AI and (later) remote players all get one; nothing in race logic is allowed
    /// to assume "index 0 is the player".
    /// </summary>
    [Serializable]
    public struct RacerId : IEquatable<RacerId>
    {
        public int Value;

        public RacerId(int value) { Value = value; }

        public static readonly RacerId None = new RacerId(0);
        public bool IsValid => Value != 0;

        public bool Equals(RacerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is RacerId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => "Racer#" + Value;
        public static bool operator ==(RacerId a, RacerId b) => a.Value == b.Value;
        public static bool operator !=(RacerId a, RacerId b) => a.Value != b.Value;
    }
}
