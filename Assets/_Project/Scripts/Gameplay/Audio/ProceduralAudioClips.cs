using UnityEngine;

namespace RedlineLegends.Audio
{
    /// <summary>
    /// Synthesised stand-in clips used whenever a VehicleAudioDefinition slot is empty, so the game
    /// is audible before recorded assets exist. Generated once per session and shared.
    /// </summary>
    public static class ProceduralAudioClips
    {
        private const int SampleRate = 22050;
        private static AudioClip _engine, _noise, _click, _impact, _hiss;

        /// <summary>One-second loop of a rich sawtooth at 55 Hz; pitch-shifted by rpm at runtime.</summary>
        public static AudioClip Engine => _engine != null ? _engine : (_engine = BuildEngine());
        public static AudioClip Noise => _noise != null ? _noise : (_noise = BuildNoise(1f, 1f));
        public static AudioClip Hiss => _hiss != null ? _hiss : (_hiss = BuildNoise(1f, 0.35f));
        public static AudioClip Click => _click != null ? _click : (_click = BuildClick());
        public static AudioClip Impact => _impact != null ? _impact : (_impact = BuildImpact());

        private static AudioClip BuildEngine()
        {
            int samples = SampleRate;
            var data = new float[samples];
            const float baseHz = 55f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float phase = t * baseHz;
                // Saw fundamental plus a couple of harmonics and a firing-order pulse.
                float saw = 2f * (phase - Mathf.Floor(phase + 0.5f));
                float h2 = Mathf.Sin(phase * 2f * Mathf.PI * 2f) * 0.35f;
                float h3 = Mathf.Sin(phase * 3f * Mathf.PI * 2f) * 0.2f;
                float pulse = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(phase * Mathf.PI * 2f * 0.5f)), 8f) * 0.4f;
                data[i] = Mathf.Clamp((saw * 0.5f + h2 + h3 + pulse) * 0.5f, -1f, 1f);
            }
            var clip = AudioClip.Create("Proc_Engine", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildNoise(float seconds, float amplitude)
        {
            int samples = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[samples];
            var rng = new System.Random(4321);
            float last = 0f;
            for (int i = 0; i < samples; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                // Light low-pass so it reads as tyre/wind rather than static.
                last = last * 0.6f + white * 0.4f;
                data[i] = last * amplitude;
            }
            var clip = AudioClip.Create("Proc_Noise", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildClick()
        {
            int samples = SampleRate / 80;
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                data[i] = Mathf.Sin(i * 0.9f) * (1f - t) * 0.6f;
            }
            var clip = AudioClip.Create("Proc_Click", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip BuildImpact()
        {
            int samples = SampleRate / 4;
            var data = new float[samples];
            var rng = new System.Random(99);
            float last = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                last = last * 0.8f + white * 0.2f;
                float thump = Mathf.Sin(t * 60f) * Mathf.Exp(-t * 18f);
                data[i] = Mathf.Clamp((last * Mathf.Exp(-t * 9f) + thump) * 0.9f, -1f, 1f);
            }
            var clip = AudioClip.Create("Proc_Impact", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
