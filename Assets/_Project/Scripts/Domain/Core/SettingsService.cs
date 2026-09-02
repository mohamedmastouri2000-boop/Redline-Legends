using System;
using RedlineLegends.Save;
using UnityEngine;

namespace RedlineLegends.Core
{
    /// <summary>
    /// Read/modify user settings and apply the parts the engine owns (quality level, frame rate).
    /// Audio and input consumers subscribe to <see cref="Changed"/> and apply their own parts.
    /// </summary>
    public sealed class SettingsService
    {
        private readonly SaveService _save;

        public event Action<SettingsData> Changed;

        public SettingsService(SaveService save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
        }

        public SettingsData Current => _save.Data.Settings;

        /// <summary>Applies a modified copy. Callers clone Current, edit, then pass it here.</summary>
        public void Apply(SettingsData updated)
        {
            if (updated == null) return;
            _save.Data.Settings = updated.Clone();
            ApplyEngineSettings(_save.Data.Settings);
            Changed?.Invoke(_save.Data.Settings);
            _save.Save();
        }

        public void ApplyEngineSettings(SettingsData settings)
        {
            int level = Mathf.Clamp((int)settings.Graphics, 0, QualitySettings.names.Length - 1);
            if (QualitySettings.GetQualityLevel() != level)
                QualitySettings.SetQualityLevel(level, true);
            Application.targetFrameRate = settings.TargetFrameRate <= 30 ? 30 : 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
