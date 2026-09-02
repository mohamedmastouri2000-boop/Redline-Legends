using RedlineLegends.Cameras;
using RedlineLegends.Core;
using RedlineLegends.Input;
using RedlineLegends.Race;
using RedlineLegends.Save;
using RedlineLegends.Utilities;
using RedlineLegends.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>
    /// In-race HUD. The vehicle block (speed, rpm, gear, nitrous, shift light) binds to the local
    /// car; race blocks (lap, position, timer) are filled by the race session through
    /// <see cref="SetRaceInfo"/>. Text is only rewritten when the value changes to avoid garbage.
    /// </summary>
    public sealed class RaceHud : MonoBehaviour
    {
        [Header("Binding")]
        [SerializeField] private MonoBehaviour localRacerSource;
        [SerializeField] private VehicleCameraRig cameraRig;

        [Header("Vehicle")]
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text unitText;
        [SerializeField] private TMP_Text gearText;
        [SerializeField] private TMP_Text rpmText;
        [SerializeField] private Image rpmFill;
        [SerializeField] private Image shiftLight;
        [SerializeField] private Image nitrousFill;
        [SerializeField] private GameObject nitrousGroup;

        [Header("Race")]
        [SerializeField] private TMP_Text lapText;
        [SerializeField] private TMP_Text positionText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private Image raceProgressFill;

        [Header("Buttons")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button cameraButton;
        [SerializeField] private Button resetButton;

        [Header("Colours")]
        [SerializeField] private Color rpmNormal = new Color(0.95f, 0.95f, 0.95f);
        [SerializeField] private Color rpmRedline = new Color(0.95f, 0.2f, 0.15f);
        [SerializeField] private Color shiftOff = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        [SerializeField] private Color shiftOn = new Color(0.2f, 1f, 0.3f, 1f);

        private VehicleController _vehicle;
        private SettingsService _settings;
        private int _lastSpeed = -1;
        private int _lastGear = int.MinValue;
        private int _lastRpm = -1;
        private string _lastLap, _lastPosition, _lastTimer, _lastInfo;
        private float _timerAccumulator;

        public event System.Action PauseRequested;

        private void Awake()
        {
            if (pauseButton != null) pauseButton.onClick.AddListener(() => PauseRequested?.Invoke());
            if (cameraButton != null) cameraButton.onClick.AddListener(() => cameraRig?.CycleMode());
            if (resetButton != null) resetButton.onClick.AddListener(() =>
            {
                if (Services.TryGet<MobileInputProvider>(out var provider)) provider.RequestReset();
            });
            if (lapText != null) lapText.text = "";
            if (positionText != null) positionText.text = "";
            if (timerText != null) timerText.text = "";
            if (infoText != null) infoText.text = "";
        }

        private void Start()
        {
            if (Services.IsReady) _settings = Services.Get<SettingsService>();
            if (localRacerSource is ILocalRacerSource source)
            {
                if (source.LocalVehicle != null) Bind(source.LocalVehicle);
                source.LocalVehicleSpawned += Bind;
            }
            if (unitText != null && _settings != null)
                unitText.text = _settings.Current.Units == SpeedUnit.Mph ? "MPH" : "KM/H";
        }

        public void Bind(VehicleController vehicle)
        {
            _vehicle = vehicle;
            if (nitrousGroup != null) nitrousGroup.SetActive(vehicle != null && vehicle.Stats.Nitrous.IsFitted);
        }

        public void SetRaceInfo(string lap, string position, string info, float progress01)
        {
            if (lapText != null && lap != _lastLap) { lapText.text = lap; _lastLap = lap; }
            if (positionText != null && position != _lastPosition) { positionText.text = position; _lastPosition = position; }
            if (infoText != null && info != _lastInfo) { infoText.text = info ?? ""; _lastInfo = info; }
            if (raceProgressFill != null) raceProgressFill.fillAmount = Mathf.Clamp01(progress01);
        }

        public void SetTimer(float seconds)
        {
            // Update the text at ~30 Hz: cheaper and still smooth for milliseconds.
            _timerAccumulator += Time.unscaledDeltaTime;
            if (_timerAccumulator < 0.033f) return;
            _timerAccumulator = 0f;
            string s = MathUtil.FormatRaceTime(seconds);
            if (timerText != null && s != _lastTimer) { timerText.text = s; _lastTimer = s; }
        }

        private void Update()
        {
            if (_vehicle == null) return;
            var tel = _vehicle.Telemetry;
            bool mph = _settings != null && _settings.Current.Units == SpeedUnit.Mph;
            int speed = Mathf.RoundToInt(mph ? tel.SpeedKmh * 0.621371f : tel.SpeedKmh);
            if (speed != _lastSpeed && speedText != null)
            {
                speedText.text = speed.ToString();
                _lastSpeed = speed;
            }
            if (tel.Gear != _lastGear && gearText != null)
            {
                gearText.text = tel.Gear < 0 ? "R" : tel.Gear == 0 ? "N" : tel.Gear.ToString();
                _lastGear = tel.Gear;
            }
            int rpm = Mathf.RoundToInt(tel.Rpm / 50f) * 50;
            if (rpm != _lastRpm && rpmText != null)
            {
                rpmText.text = rpm.ToString();
                _lastRpm = rpm;
            }
            if (rpmFill != null)
            {
                rpmFill.fillAmount = tel.RpmNormalized;
                rpmFill.color = tel.RpmNormalized > 0.9f || tel.LimiterActive ? rpmRedline : rpmNormal;
            }
            if (shiftLight != null)
            {
                bool on = tel.Gear > 0 && tel.Gear < tel.GearCount && tel.RpmNormalized >= 0.9f;
                shiftLight.color = on ? shiftOn : shiftOff;
            }
            if (nitrousFill != null) nitrousFill.fillAmount = tel.Nitrous01;
        }

#if UNITY_EDITOR
        public void EditorWire(MonoBehaviour source, VehicleCameraRig rig, TMP_Text speed, TMP_Text unit, TMP_Text gear, TMP_Text rpm,
            Image rpmBar, Image shift, Image nos, GameObject nosGroup, TMP_Text lap, TMP_Text position, TMP_Text timer, TMP_Text info,
            Image progress, Button pause, Button camera, Button reset)
        {
            localRacerSource = source; cameraRig = rig; speedText = speed; unitText = unit; gearText = gear; rpmText = rpm; rpmFill = rpmBar;
            shiftLight = shift; nitrousFill = nos; nitrousGroup = nosGroup; lapText = lap; positionText = position; timerText = timer;
            infoText = info; raceProgressFill = progress; pauseButton = pause; cameraButton = camera; resetButton = reset;
        }
#endif
    }
}
