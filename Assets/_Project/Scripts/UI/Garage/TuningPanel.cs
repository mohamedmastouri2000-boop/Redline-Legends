using System;
using RedlineLegends.Core;
using RedlineLegends.Progression;
using RedlineLegends.Tuning;
using RedlineLegends.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>
    /// Tuning sliders for the owned car in the garage. Edits a working copy and shows the live
    /// performance rating; APPLY writes it through GarageService (which saves). Gear ratio rows
    /// unlock with the advanced tuning level.
    /// </summary>
    public sealed class TuningPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text ratingText;
        [SerializeField] private TMP_Text lockText;
        [SerializeField] private SliderRow finalDrive;
        [SerializeField] private SliderRow suspension;
        [SerializeField] private SliderRow rideHeight;
        [SerializeField] private SliderRow gripBias;
        [SerializeField] private SliderRow nitrousBalance;
        [SerializeField] private SliderRow[] gearRows;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;

        private GarageService _garage;
        private VehicleDefinition _vehicle;
        private VehicleTuningData _working;
        private bool _loading;

        public event Action Applied;
        public Button CloseButton => closeButton;

        private void Awake()
        {
            if (applyButton != null) applyButton.onClick.AddListener(Apply);
            if (resetButton != null) resetButton.onClick.AddListener(ResetToDefault);
            // Subscribe once; Bind() only refreshes values.
            finalDrive.Changed += v => Edit(t => t.FinalDrive = v);
            suspension.Changed += v => Edit(t => t.SuspensionStiffness = v);
            rideHeight.Changed += v => Edit(t => t.RideHeight = v);
            gripBias.Changed += v => Edit(t => t.GripBias = v);
            nitrousBalance.Changed += v => Edit(t => t.NitrousBalance = v);
            for (int i = 0; i < gearRows.Length; i++)
            {
                int index = i;
                gearRows[i].Changed += v => Edit(t => { if (index < t.GearRatios.Length) t.GearRatios[index] = v; });
            }
        }

        public void Open(VehicleDefinition vehicle)
        {
            if (!Services.IsReady || vehicle == null) return;
            _garage = Services.Get<GarageService>();
            _vehicle = vehicle;
            _working = _garage.GetTuning(vehicle.Id).Clone();
            gameObject.SetActive(true);
            titleText.text = "TUNING  " + vehicle.DisplayName;
            Bind();
            RefreshRating();
        }

        private void Bind()
        {
            _loading = true;
            finalDrive.Setup("Final drive (short / tall)", -1f, 1f, _working.FinalDrive);
            suspension.Setup("Suspension (soft / stiff)", -1f, 1f, _working.SuspensionStiffness);
            rideHeight.Setup("Ride height (low / high)", -1f, 1f, _working.RideHeight);
            gripBias.Setup("Grip bias (front / rear)", -1f, 1f, _working.GripBias);
            nitrousBalance.Setup("Nitrous (duration / power)", 0f, 1f, _working.NitrousBalance);
            bool advanced = _garage.IsAdvancedTuningUnlocked;
            int gearCount = _vehicle.BaseStats.Transmission.GearCount;
            if (_working.GearRatios == null || _working.GearRatios.Length < gearCount)
            {
                var ratios = new float[gearCount];
                if (_working.GearRatios != null) Array.Copy(_working.GearRatios, ratios, _working.GearRatios.Length);
                _working.GearRatios = ratios;
            }
            for (int i = 0; i < gearRows.Length; i++)
            {
                bool show = advanced && i < gearCount;
                gearRows[i].gameObject.SetActive(show);
                if (show) gearRows[i].Setup("Gear " + (i + 1) + " (short / tall)", -1f, 1f, _working.GearRatios[i]);
            }
            if (lockText != null)
            {
                lockText.gameObject.SetActive(!advanced);
                lockText.text = "Gear ratio tuning unlocks at level " + Services.Get<GameConfig>().ProgressionConfig.AdvancedTuningLevel;
            }
            _loading = false;
        }

        private void Edit(Action<VehicleTuningData> mutate)
        {
            if (_loading || _working == null) return;
            mutate(_working);
            RefreshRating();
        }

        private void RefreshRating()
        {
            if (_vehicle == null || ratingText == null) return;
            var owned = _garage.GetOwned(_vehicle.Id);
            var spec = VehicleSpecBuilder.Build(_vehicle, owned?.UpgradeStages, _working);
            var b = PerformanceRatingCalculator.ComputeBreakdown(spec.Stats);
            ratingText.text = "PR " + spec.PerformanceRating + "   0-100 " + b.ZeroToHundredSeconds.ToString("0.0") + " s   top " + Mathf.RoundToInt(b.TopSpeedKmh) + " km/h";
        }

        private void Apply()
        {
            if (_vehicle == null || _working == null) return;
            _garage.SetTuning(_vehicle.Id, _working);
            Applied?.Invoke();
        }

        private void ResetToDefault()
        {
            _working = VehicleTuningData.Default();
            Bind();
            RefreshRating();
        }

#if UNITY_EDITOR
        public void EditorWire(TMP_Text title, TMP_Text rating, TMP_Text locked, SliderRow fd, SliderRow susp, SliderRow ride, SliderRow grip,
            SliderRow nos, SliderRow[] gears, Button apply, Button reset, Button close)
        {
            titleText = title; ratingText = rating; lockText = locked; finalDrive = fd; suspension = susp; rideHeight = ride; gripBias = grip;
            nitrousBalance = nos; gearRows = gears; applyButton = apply; resetButton = reset; closeButton = close;
        }
#endif
    }
}
