using System;
using RedlineLegends.Core;
using RedlineLegends.Save;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>
    /// Settings screen. Every change is applied immediately through SettingsService (which saves),
    /// so there is no apply button to forget on a phone.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private CycleRow controlStyle;
        [SerializeField] private CycleRow transmission;
        [SerializeField] private CycleRow cameraMode;
        [SerializeField] private CycleRow graphics;
        [SerializeField] private CycleRow frameRate;
        [SerializeField] private CycleRow units;
        [SerializeField] private CycleRow vibration;
        [SerializeField] private CycleRow tutorials;
        [SerializeField] private SliderRow steeringSensitivity;
        [SerializeField] private SliderRow tiltSensitivity;
        [SerializeField] private SliderRow cameraShake;
        [SerializeField] private SliderRow masterVolume;
        [SerializeField] private SliderRow musicVolume;
        [SerializeField] private SliderRow sfxVolume;
        [SerializeField] private Button backButton;

        private SettingsService _settings;
        private bool _loading;

        public Button BackButton => backButton;

        private void Start()
        {
            if (!Services.IsReady) return;
            _settings = Services.Get<SettingsService>();
            var s = _settings.Current;
            _loading = true;
            controlStyle.Setup("Control style", new[] { "Buttons", "Steering wheel", "Tilt" }, (int)s.ControlStyle);
            transmission.Setup("Gearbox", new[] { "Automatic", "Manual" }, (int)s.Transmission);
            cameraMode.Setup("Camera", new[] { "Chase", "Hood", "Cockpit" }, (int)s.Camera);
            graphics.Setup("Graphics", new[] { "Low", "Medium", "High" }, (int)s.Graphics);
            frameRate.Setup("Frame rate", new[] { "30 FPS", "60 FPS" }, s.TargetFrameRate <= 30 ? 0 : 1);
            units.Setup("Units", new[] { "km/h", "mph" }, (int)s.Units);
            vibration.Setup("Vibration", new[] { "Off", "On" }, s.Vibration ? 1 : 0);
            tutorials.Setup("Tutorials", new[] { "Off", "On" }, s.TutorialsEnabled ? 1 : 0);
            steeringSensitivity.Setup("Steering sensitivity", 0.5f, 2f, s.SteeringSensitivity);
            tiltSensitivity.Setup("Tilt sensitivity", 0.5f, 2f, s.TiltSensitivity);
            cameraShake.Setup("Camera shake", 0f, 1f, s.CameraShake);
            masterVolume.Setup("Master volume", 0f, 1f, s.MasterVolume);
            musicVolume.Setup("Music volume", 0f, 1f, s.MusicVolume);
            sfxVolume.Setup("Effects volume", 0f, 1f, s.SfxVolume);
            _loading = false;

            controlStyle.Changed += i => Apply(d => d.ControlStyle = (ControlStyle)i);
            transmission.Changed += i => Apply(d => d.Transmission = (TransmissionMode)i);
            cameraMode.Changed += i => Apply(d => d.Camera = (CameraMode)i);
            graphics.Changed += i => Apply(d => d.Graphics = (GraphicsPreset)i);
            frameRate.Changed += i => Apply(d => d.TargetFrameRate = i == 0 ? 30 : 60);
            units.Changed += i => Apply(d => d.Units = (SpeedUnit)i);
            vibration.Changed += i => Apply(d => d.Vibration = i == 1);
            tutorials.Changed += i => Apply(d => d.TutorialsEnabled = i == 1);
            steeringSensitivity.Changed += v => Apply(d => d.SteeringSensitivity = v);
            tiltSensitivity.Changed += v => Apply(d => d.TiltSensitivity = v);
            cameraShake.Changed += v => Apply(d => d.CameraShake = v);
            masterVolume.Changed += v => Apply(d => d.MasterVolume = v);
            musicVolume.Changed += v => Apply(d => d.MusicVolume = v);
            sfxVolume.Changed += v => Apply(d => d.SfxVolume = v);
        }

        private void Apply(Action<SettingsData> mutate)
        {
            if (_loading || _settings == null) return;
            var copy = _settings.Current.Clone();
            mutate(copy);
            _settings.Apply(copy);
        }

#if UNITY_EDITOR
        public void EditorWire(CycleRow style, CycleRow gearbox, CycleRow cam, CycleRow gfx, CycleRow fps, CycleRow unit, CycleRow vib,
            CycleRow tut, SliderRow steer, SliderRow tilt, SliderRow shake, SliderRow master, SliderRow music, SliderRow sfx, Button back)
        {
            controlStyle = style; transmission = gearbox; cameraMode = cam; graphics = gfx; frameRate = fps; units = unit; vibration = vib;
            tutorials = tut; steeringSensitivity = steer; tiltSensitivity = tilt; cameraShake = shake; masterVolume = master;
            musicVolume = music; sfxVolume = sfx; backButton = back;
        }
#endif
    }
}
