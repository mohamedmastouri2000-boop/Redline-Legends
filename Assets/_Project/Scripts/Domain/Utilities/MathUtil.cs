using UnityEngine;

namespace RedlineLegends.Utilities
{
    public static class MathUtil
    {
        public const float MsToKmh = 3.6f;
        public const float KmhToMs = 1f / 3.6f;
        public const float MsToMph = 2.23694f;
        public const float HpToWatts = 745.7f;
        public const float RpmToRadPerSec = 2f * Mathf.PI / 60f;
        public const float RadPerSecToRpm = 60f / (2f * Mathf.PI);

        /// <summary>Frame-rate independent exponential approach from current towards target.</summary>
        public static float Damp(float current, float target, float sharpness, float deltaTime)
            => Mathf.Lerp(current, target, 1f - Mathf.Exp(-sharpness * deltaTime));

        public static Vector3 Damp(Vector3 current, Vector3 target, float sharpness, float deltaTime)
            => Vector3.Lerp(current, target, 1f - Mathf.Exp(-sharpness * deltaTime));

        public static float Remap(float value, float inMin, float inMax, float outMin, float outMax)
        {
            if (Mathf.Approximately(inMax, inMin)) return outMin;
            float t = Mathf.InverseLerp(inMin, inMax, value);
            return Mathf.Lerp(outMin, outMax, t);
        }

        /// <summary>Projects a point onto segment AB. Returns parameter t in [0,1] and the closest point.</summary>
        public static float ProjectOnSegment(Vector3 point, Vector3 a, Vector3 b, out Vector3 closest)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 1e-6f)
            {
                closest = a;
                return 0f;
            }
            float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / len2);
            closest = a + ab * t;
            return t;
        }

        public static string FormatRaceTime(float seconds)
        {
            if (seconds < 0f || float.IsInfinity(seconds) || float.IsNaN(seconds)) return "--:--.---";
            int minutes = (int)(seconds / 60f);
            float rem = seconds - minutes * 60f;
            return minutes.ToString("00") + ":" + rem.ToString("00.000");
        }
    }
}
