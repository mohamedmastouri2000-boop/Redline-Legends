using System;
using RedlineLegends.Save;
using UnityEngine;

namespace RedlineLegends.Core
{
    /// <summary>
    /// Master/music/effects volume. Master drives the AudioListener; music and effects are
    /// multipliers that every AudioSource owner applies to its own base volume, so no mixer asset
    /// or per-source registry is needed.
    /// </summary>
    public sealed class AudioService : IDisposable
    {
        private readonly SettingsService _settings;

        public float Master { get; private set; } = 1f;
        public float Music { get; private set; } = 0.7f;
        public float Sfx { get; private set; } = 1f;

        public event Action Changed;

        public AudioService(SettingsService settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settings.Changed += Apply;
            Apply(_settings.Current);
        }

        public void Dispose() => _settings.Changed -= Apply;

        private void Apply(SettingsData settings)
        {
            Master = Mathf.Clamp01(settings.MasterVolume);
            Music = Mathf.Clamp01(settings.MusicVolume);
            Sfx = Mathf.Clamp01(settings.SfxVolume);
            AudioListener.volume = Master;
            Changed?.Invoke();
        }
    }
}
