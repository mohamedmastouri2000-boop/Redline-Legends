using RedlineLegends.Save;
using UnityEngine;

namespace RedlineLegends.Core
{
    /// <summary>
    /// Vibration feedback gated by the user setting. Android exposes only a fixed pulse through
    /// Handheld.Vibrate, so strength is expressed as a minimum interval between pulses.
    /// </summary>
    public sealed class HapticsService
    {
        private readonly SettingsService _settings;
        private float _lastPulseTime = -10f;

        public HapticsService(SettingsService settings)
        {
            _settings = settings;
        }

        public bool Enabled => _settings != null && _settings.Current.Vibration;

        /// <summary>strength01 controls how often pulses may repeat (1 = every 80 ms, 0 = every 400 ms).</summary>
        public void Pulse(float strength01)
        {
            if (!Enabled) return;
            float minInterval = Mathf.Lerp(0.4f, 0.08f, Mathf.Clamp01(strength01));
            if (Time.unscaledTime - _lastPulseTime < minInterval) return;
            _lastPulseTime = Time.unscaledTime;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
