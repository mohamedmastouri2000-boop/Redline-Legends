using System;
using System.Collections.Generic;
using RedlineLegends.Content;
using RedlineLegends.Core;
using RedlineLegends.Save;
using RedlineLegends.Tuning;
using RedlineLegends.Upgrades;
using RedlineLegends.Vehicles;

namespace RedlineLegends.Progression
{
    /// <summary>
    /// Vehicle ownership, selection, installed upgrades, tuning and paint. Produces the resolved
    /// VehicleSpec for the player's cars. Purchases go through the profile's credit balance.
    /// </summary>
    public sealed class GarageService
    {
        private static readonly int CategoryCount = Enum.GetValues(typeof(UpgradeCategory)).Length;

        private readonly SaveService _save;
        private readonly ContentCatalog _catalog;
        private readonly PlayerProfileService _profile;
        private readonly ProgressionConfig _config;

        public event Action Changed;
        public event Action<string> VehiclePurchased;
        public event Action<string> SelectionChanged;

        public GarageService(SaveService save, ContentCatalog catalog, PlayerProfileService profile, ProgressionConfig config)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            EnsureStarterVehicle();
        }

        private GarageData Data => _save.Data.Garage;
        public IReadOnlyList<OwnedVehicleData> Owned => Data.Owned;
        public string SelectedVehicleId => Data.SelectedVehicleId;

        public VehicleDefinition SelectedVehicle
            => _catalog.TryGetVehicle(Data.SelectedVehicleId, out var def) ? def : null;

        /// <summary>Guarantees a drivable car after a fresh profile or a content change that removed a car.</summary>
        private void EnsureStarterVehicle()
        {
            // Drop owned entries whose definitions no longer exist (content removed in an update).
            for (int i = Data.Owned.Count - 1; i >= 0; i--)
                if (!_catalog.TryGetVehicle(Data.Owned[i].VehicleId, out _))
                    Data.Owned.RemoveAt(i);

            if (Data.Owned.Count == 0)
            {
                string starterId = _config.StarterVehicleId;
                if (!_catalog.TryGetVehicle(starterId, out _) && _catalog.Vehicles.Count > 0)
                    starterId = _catalog.Vehicles[0].Id;
                if (!string.IsNullOrEmpty(starterId))
                    Data.Owned.Add(NewOwned(starterId));
            }

            if (GetOwned(Data.SelectedVehicleId) == null && Data.Owned.Count > 0)
                Data.SelectedVehicleId = Data.Owned[0].VehicleId;
        }

        private static OwnedVehicleData NewOwned(string vehicleId)
        {
            return new OwnedVehicleData
            {
                VehicleId = vehicleId,
                UpgradeStages = new int[CategoryCount],
                Tuning = VehicleTuningData.Default(),
                PurchasedUtcTicks = DateTime.UtcNow.Ticks
            };
        }

        public bool IsOwned(string vehicleId) => GetOwned(vehicleId) != null;

        public OwnedVehicleData GetOwned(string vehicleId)
        {
            if (string.IsNullOrEmpty(vehicleId)) return null;
            var list = Data.Owned;
            for (int i = 0; i < list.Count; i++)
                if (list[i].VehicleId == vehicleId) return list[i];
            return null;
        }

        public bool Select(string vehicleId)
        {
            if (!IsOwned(vehicleId) || Data.SelectedVehicleId == vehicleId) return false;
            Data.SelectedVehicleId = vehicleId;
            SelectionChanged?.Invoke(vehicleId);
            Changed?.Invoke();
            _save.Save();
            return true;
        }

        public bool CanPurchase(VehicleDefinition definition, IProgressQuery progress)
        {
            if (definition == null || IsOwned(definition.Id)) return false;
            if (!definition.UnlockRequirement.IsMet(progress)) return false;
            return _profile.CanAfford(definition.Price);
        }

        public bool TryPurchase(VehicleDefinition definition, IProgressQuery progress)
        {
            if (!CanPurchase(definition, progress)) return false;
            if (!_profile.TrySpendCredits(definition.Price)) return false;
            Data.Owned.Add(NewOwned(definition.Id));
            Data.SelectedVehicleId = definition.Id;
            VehiclePurchased?.Invoke(definition.Id);
            SelectionChanged?.Invoke(definition.Id);
            Changed?.Invoke();
            _save.Save();
            return true;
        }

        public int GetUpgradeStage(string vehicleId, UpgradeCategory category)
        {
            var owned = GetOwned(vehicleId);
            if (owned == null) return 0;
            EnsureStageArray(owned);
            return owned.UpgradeStages[(int)category];
        }

        /// <summary>Price of the next stage, or -1 when maxed / unavailable.</summary>
        public int GetNextUpgradePrice(string vehicleId, UpgradeCategory category)
        {
            if (!_catalog.TryGetVehicle(vehicleId, out var def)) return -1;
            var upgrade = def.FindUpgrade(category);
            if (upgrade == null) return -1;
            var stage = upgrade.GetStage(GetUpgradeStage(vehicleId, category) + 1);
            return stage != null ? stage.Price : -1;
        }

        public bool TryInstallNextUpgrade(string vehicleId, UpgradeCategory category)
        {
            var owned = GetOwned(vehicleId);
            if (owned == null || !_catalog.TryGetVehicle(vehicleId, out var def)) return false;
            var upgrade = def.FindUpgrade(category);
            if (upgrade == null) return false;
            EnsureStageArray(owned);
            int next = owned.UpgradeStages[(int)category] + 1;
            var stage = upgrade.GetStage(next);
            if (stage == null) return false;
            if (_profile.Level < stage.RequiredPlayerLevel) return false;
            if (!_profile.TrySpendCredits(stage.Price)) return false;
            owned.UpgradeStages[(int)category] = next;
            Changed?.Invoke();
            _save.Save();
            return true;
        }

        public VehicleTuningData GetTuning(string vehicleId)
        {
            var owned = GetOwned(vehicleId);
            return owned != null ? owned.Tuning : VehicleTuningData.Default();
        }

        public bool IsAdvancedTuningUnlocked => _profile.Level >= _config.AdvancedTuningLevel;

        public void SetTuning(string vehicleId, VehicleTuningData tuning)
        {
            var owned = GetOwned(vehicleId);
            if (owned == null || tuning == null) return;
            owned.Tuning = tuning.Clone();
            Changed?.Invoke();
            _save.Save();
        }

        public bool TrySetPaint(string vehicleId, int paintIndex)
        {
            var owned = GetOwned(vehicleId);
            if (owned == null || !_catalog.TryGetVehicle(vehicleId, out var def)) return false;
            if (paintIndex < 0 || paintIndex >= def.PaintOptions.Length) return false;
            if (owned.PaintIndex == paintIndex) return true;
            int price = def.PaintOptions[paintIndex].Price;
            if (price > 0 && !_profile.TrySpendCredits(price)) return false;
            owned.PaintIndex = paintIndex;
            Changed?.Invoke();
            _save.Save();
            return true;
        }

        public void AddOdometer(string vehicleId, float meters)
        {
            var owned = GetOwned(vehicleId);
            if (owned != null && meters > 0f) owned.OdometerMeters += meters;
        }

        /// <summary>Resolved spec with the player's upgrades and tuning; stock if not owned.</summary>
        public VehicleSpec BuildSpec(string vehicleId)
        {
            if (!_catalog.TryGetVehicle(vehicleId, out var def))
            {
                GameLog.Error("BuildSpec: unknown vehicle '" + vehicleId + "'.");
                return null;
            }
            var owned = GetOwned(vehicleId);
            if (owned == null) return VehicleSpecBuilder.BuildStock(def);
            EnsureStageArray(owned);
            return VehicleSpecBuilder.Build(def, owned.UpgradeStages, owned.Tuning);
        }

        public VehicleSpec BuildSelectedSpec() => BuildSpec(Data.SelectedVehicleId);

        private static void EnsureStageArray(OwnedVehicleData owned)
        {
            if (owned.UpgradeStages != null && owned.UpgradeStages.Length >= CategoryCount) return;
            var stages = new int[CategoryCount];
            if (owned.UpgradeStages != null)
                Array.Copy(owned.UpgradeStages, stages, owned.UpgradeStages.Length);
            owned.UpgradeStages = stages;
        }
    }
}
