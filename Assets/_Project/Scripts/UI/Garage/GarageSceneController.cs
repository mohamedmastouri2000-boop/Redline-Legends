using System.Collections.Generic;
using RedlineLegends.Content;
using RedlineLegends.Core;
using RedlineLegends.Progression;
using RedlineLegends.Upgrades;
using RedlineLegends.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>
    /// 3D garage: shows the browsed car on a turntable, lets the player rotate it by dragging,
    /// select/purchase it, and install upgrades. Browsing order is the content database order so
    /// new cars appear without UI changes.
    /// </summary>
    public sealed class GarageSceneController : MonoBehaviour, IDragHandler
    {
        [Header("Scene")]
        [SerializeField] private Transform turntable;
        [SerializeField] private float autoRotateSpeed = 8f;
        [SerializeField] private float dragRotateSpeed = 0.3f;

        [Header("UI")]
        [SerializeField] private TMP_Text carNameText;
        [SerializeField] private TMP_Text carClassText;
        [SerializeField] private TMP_Text ratingText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text creditsText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionLabel;
        [SerializeField] private Button backButton;
        [SerializeField] private Button testDriveButton;
        [SerializeField] private Button tuneButton;
        [SerializeField] private TuningPanel tuningPanel;
        [SerializeField] private Button paintPrevButton;
        [SerializeField] private Button paintNextButton;
        [SerializeField] private TMP_Text paintText;
        [SerializeField] private RectTransform upgradeList;
        [SerializeField] private UpgradeRow upgradeRowTemplate;

        private ContentCatalog _catalog;
        private GarageService _garage;
        private PlayerProfileService _profile;
        private ProgressionService _progression;
        private SceneFlowService _sceneFlow;
        private readonly List<UpgradeRow> _upgradeRows = new List<UpgradeRow>();

        private int _index;
        private GameObject _displayed;
        private float _yaw;
        private float _idleTimer;

        private void Start()
        {
            if (!Services.IsReady)
            {
                GameLog.Error("Garage opened before GameBootstrap.");
                return;
            }
            _catalog = Services.Get<ContentCatalog>();
            _garage = Services.Get<GarageService>();
            _profile = Services.Get<PlayerProfileService>();
            _progression = Services.Get<ProgressionService>();
            _sceneFlow = Services.Get<SceneFlowService>();

            prevButton.onClick.AddListener(() => Browse(-1));
            nextButton.onClick.AddListener(() => Browse(1));
            actionButton.onClick.AddListener(OnAction);
            backButton.onClick.AddListener(() => _sceneFlow.LoadMainMenu());
            if (testDriveButton != null) testDriveButton.onClick.AddListener(LaunchTestDrive);
            if (tuneButton != null && tuningPanel != null)
            {
                tuneButton.onClick.AddListener(() => { if (_garage.IsOwned(Current.Id)) tuningPanel.Open(Current); });
                tuningPanel.CloseButton.onClick.AddListener(() => tuningPanel.gameObject.SetActive(false));
                tuningPanel.Applied += Refresh;
                tuningPanel.gameObject.SetActive(false);
            }
            if (paintPrevButton != null) paintPrevButton.onClick.AddListener(() => CyclePaint(-1));
            if (paintNextButton != null) paintNextButton.onClick.AddListener(() => CyclePaint(1));
            upgradeRowTemplate.gameObject.SetActive(false);

            _index = IndexOf(_garage.SelectedVehicleId);
            _profile.Changed += Refresh;
            _garage.Changed += Refresh;
            ShowCurrent();
        }

        private void OnDestroy()
        {
            if (_profile != null) _profile.Changed -= Refresh;
            if (_garage != null) _garage.Changed -= Refresh;
        }

        private void Update()
        {
            _idleTimer += Time.deltaTime;
            if (_idleTimer > 2f) _yaw += autoRotateSpeed * Time.deltaTime;
            if (turntable != null) turntable.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _yaw -= eventData.delta.x * dragRotateSpeed;
            _idleTimer = 0f;
        }

        private int IndexOf(string vehicleId)
        {
            for (int i = 0; i < _catalog.Vehicles.Count; i++)
                if (_catalog.Vehicles[i].Id == vehicleId) return i;
            return 0;
        }

        private VehicleDefinition Current => _catalog.Vehicles.Count > 0 ? _catalog.Vehicles[_index] : null;

        private void Browse(int delta)
        {
            int count = _catalog.Vehicles.Count;
            if (count == 0) return;
            _index = (_index + delta + count) % count;
            ShowCurrent();
        }

        private void ShowCurrent()
        {
            var def = Current;
            if (def == null) return;

            if (_displayed != null) Destroy(_displayed);
            if (def.VisualPrefab != null)
            {
                _displayed = Instantiate(def.VisualPrefab, turntable);
                _displayed.transform.localPosition = Vector3.zero;
                _displayed.transform.localRotation = Quaternion.identity;
                int paint = _garage.GetOwned(def.Id)?.PaintIndex ?? 0;
                VehicleVisualUtility.ApplyPaint(_displayed, def, paint);
            }
            Refresh();
        }

        private void Refresh()
        {
            var def = Current;
            if (def == null) return;
            var spec = _garage.BuildSpec(def.Id);
            var breakdown = PerformanceRatingCalculator.ComputeBreakdown(spec.Stats);
            bool owned = _garage.IsOwned(def.Id);
            bool selected = _garage.SelectedVehicleId == def.Id;

            carNameText.text = def.BrandName + " " + def.DisplayName;
            carClassText.text = def.VehicleClass.ToString().ToUpperInvariant() + " CLASS";
            ratingText.text = "PR " + spec.PerformanceRating;
            statsText.text =
                Mathf.RoundToInt(spec.Stats.Engine.PeakPowerHp * spec.Stats.Engine.TurboBoostMultiplier) + " hp · " +
                Mathf.RoundToInt(spec.Stats.Chassis.MassKg) + " kg · " + spec.Stats.Transmission.Drivetrain + "\n" +
                "0-100: " + breakdown.ZeroToHundredSeconds.ToString("0.0") + " s · Top: " +
                Mathf.RoundToInt(breakdown.TopSpeedKmh) + " km/h\n" +
                "Grip " + spec.Stats.Tires.LateralGrip.ToString("0.00") + " · Braking " + breakdown.BrakingG.ToString("0.00") + " g";
            creditsText.text = _profile.Credits.ToString("N0") + " CR";

            if (selected)
            {
                actionLabel.text = "SELECTED";
                actionButton.interactable = false;
                statusText.text = "This is your current car.";
            }
            else if (owned)
            {
                actionLabel.text = "SELECT";
                actionButton.interactable = true;
                statusText.text = "Owned";
            }
            else
            {
                bool unlocked = def.UnlockRequirement.IsMet(_progression);
                actionLabel.text = "BUY  " + def.Price.ToString("N0") + " CR";
                actionButton.interactable = unlocked && _profile.CanAfford(def.Price);
                statusText.text = !unlocked ? "Locked: " + def.UnlockRequirement.Describe()
                    : _profile.CanAfford(def.Price) ? "Available" : "Not enough credits";
            }

            RefreshUpgrades(def, owned);

            if (tuneButton != null) tuneButton.interactable = owned;
            int paintIndex = owned ? _garage.GetOwned(def.Id).PaintIndex : 0;
            if (paintText != null && def.PaintOptions.Length > 0)
            {
                var paint = def.PaintOptions[Mathf.Clamp(paintIndex, 0, def.PaintOptions.Length - 1)];
                paintText.text = paint.Name + (paint.Price > 0 && owned ? "  " + paint.Price.ToString("N0") + " CR" : "");
            }
            if (paintPrevButton != null) paintPrevButton.interactable = owned && def.PaintOptions.Length > 1;
            if (paintNextButton != null) paintNextButton.interactable = owned && def.PaintOptions.Length > 1;
        }

        private void CyclePaint(int delta)
        {
            var def = Current;
            if (def == null || !_garage.IsOwned(def.Id) || def.PaintOptions.Length < 2) return;
            int count = def.PaintOptions.Length;
            int index = (_garage.GetOwned(def.Id).PaintIndex + delta + count) % count;
            if (!_garage.TrySetPaint(def.Id, index))
            {
                statusText.text = "Not enough credits for that paint.";
                return;
            }
            if (_displayed != null) VehicleVisualUtility.ApplyPaint(_displayed, def, index);
            Refresh();
        }

        private void RefreshUpgrades(VehicleDefinition def, bool owned)
        {
            var slots = def.UpgradeSlots;
            int used = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot.Definition == null) continue;
                var row = GetUpgradeRow(used++);
                int stage = _garage.GetUpgradeStage(def.Id, slot.Category);
                int price = _garage.GetNextUpgradePrice(def.Id, slot.Category);
                var next = slot.Definition.GetStage(stage + 1);
                bool canBuy = owned && next != null && _profile.CanAfford(price) && _profile.Level >= next.RequiredPlayerLevel;
                string lockText = next != null && _profile.Level < next.RequiredPlayerLevel ? "LVL " + next.RequiredPlayerLevel : null;
                var category = slot.Category;
                row.Set(slot.Definition.DisplayName, stage, slot.Definition.MaxStage, price, canBuy, lockText,
                    () => _garage.TryInstallNextUpgrade(def.Id, category));
            }
            for (int i = used; i < _upgradeRows.Count; i++) _upgradeRows[i].gameObject.SetActive(false);
        }

        private UpgradeRow GetUpgradeRow(int index)
        {
            while (_upgradeRows.Count <= index)
            {
                var row = Instantiate(upgradeRowTemplate, upgradeList);
                row.name = "Upgrade" + _upgradeRows.Count;
                _upgradeRows.Add(row);
            }
            _upgradeRows[index].gameObject.SetActive(true);
            return _upgradeRows[index];
        }

        private void OnAction()
        {
            var def = Current;
            if (def == null) return;
            if (_garage.IsOwned(def.Id)) _garage.Select(def.Id);
            else if (!_garage.TryPurchase(def, _progression)) statusText.text = "Purchase failed.";
            Refresh();
        }

        private void LaunchTestDrive()
        {
            var config = Services.Get<GameConfig>();
            if (!_catalog.TryGetTrack(config.TestDriveTrackId, out var track))
            {
                statusText.text = "Test drive track is not available.";
                return;
            }
            var builder = new Race.RaceLaunchBuilder(_catalog, _garage, _profile);
            var request = builder.BuildPractice(track);
            if (request != null) _sceneFlow.LoadRace(request);
        }

#if UNITY_EDITOR
        public void EditorWire(Transform table, TMP_Text name, TMP_Text cls, TMP_Text rating, TMP_Text stats, TMP_Text credits,
            TMP_Text status, Button prev, Button next, Button action, TMP_Text actionText, Button back, Button testDrive,
            RectTransform upgrades, UpgradeRow template, Button tune, TuningPanel tuning, Button paintPrev, Button paintNext, TMP_Text paint)
        {
            turntable = table; carNameText = name; carClassText = cls; ratingText = rating; statsText = stats;
            creditsText = credits; statusText = status; prevButton = prev; nextButton = next; actionButton = action;
            actionLabel = actionText; backButton = back; testDriveButton = testDrive; upgradeList = upgrades; upgradeRowTemplate = template;
            tuneButton = tune; tuningPanel = tuning; paintPrevButton = paintPrev; paintNextButton = paintNext; paintText = paint;
        }
#endif
    }
}
