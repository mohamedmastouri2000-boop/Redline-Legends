using System;
using RedlineLegends.Audio;
using RedlineLegends.Progression;
using RedlineLegends.Tuning;
using RedlineLegends.Upgrades;
using UnityEngine;

namespace RedlineLegends.Vehicles
{
    [Serializable]
    public sealed class PaintOption
    {
        public string Name = "Factory";
        public Color Color = Color.white;
        [Range(0f, 1f)] public float Metallic = 0.6f;
        [Range(0f, 1f)] public float Smoothness = 0.85f;
        public int Price = 0;
    }

    [Serializable]
    public sealed class UpgradeSlot
    {
        public UpgradeCategory Category;
        public VehicleUpgradeDefinition Definition;
    }

    /// <summary>
    /// Static description of a purchasable car. Immutable at runtime; player-owned state
    /// (upgrades, tuning, paint) lives in the save file keyed by <see cref="Id"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "veh_new", menuName = "Redline Legends/Vehicle Definition")]
    public sealed class VehicleDefinition : ScriptableObject
    {
        [SerializeField] private string id = "veh_new";
        [SerializeField] private string displayName = "New Vehicle";
        [SerializeField] private string brandName = "Redline";
        [SerializeField] private VehicleClass vehicleClass = VehicleClass.Street;
        [SerializeField] private int price = 15000;
        [SerializeField] private UnlockRequirement unlockRequirement;
        [SerializeField] private VehicleStats baseStats = new VehicleStats();
        [SerializeField] private UpgradeSlot[] upgradeSlots = Array.Empty<UpgradeSlot>();
        [SerializeField] private TuningLimits tuningLimits = new TuningLimits();
        [SerializeField] private PaintOption[] paintOptions = { new PaintOption() };
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private VehicleAudioDefinition audio;
        [SerializeField] private Sprite thumbnail;
        [SerializeField] private bool supportsCockpitCamera;

        public string Id => id;
        public string DisplayName => displayName;
        public string BrandName => brandName;
        public VehicleClass VehicleClass => vehicleClass;
        public int Price => price;
        public UnlockRequirement UnlockRequirement => unlockRequirement;
        public VehicleStats BaseStats => baseStats;
        public UpgradeSlot[] UpgradeSlots => upgradeSlots;
        public TuningLimits TuningLimits => tuningLimits;
        public PaintOption[] PaintOptions => paintOptions;
        public GameObject VisualPrefab => visualPrefab;
        public VehicleAudioDefinition Audio => audio;
        public Sprite Thumbnail => thumbnail;
        public bool SupportsCockpitCamera => supportsCockpitCamera;

        public VehicleUpgradeDefinition FindUpgrade(UpgradeCategory category)
        {
            for (int i = 0; i < upgradeSlots.Length; i++)
                if (upgradeSlots[i].Category == category)
                    return upgradeSlots[i].Definition;
            return null;
        }

#if UNITY_EDITOR
        /// <summary>Editor-only authoring helper used by the content generator.</summary>
        public void EditorInitialize(string newId, string newDisplayName, string newBrand, VehicleClass cls, int newPrice,
            VehicleStats stats, UpgradeSlot[] slots, PaintOption[] paints, GameObject prefab, VehicleAudioDefinition audioDef,
            UnlockRequirement unlock)
        {
            id = newId;
            displayName = newDisplayName;
            brandName = newBrand;
            vehicleClass = cls;
            price = newPrice;
            baseStats = stats;
            upgradeSlots = slots;
            paintOptions = paints;
            visualPrefab = prefab;
            audio = audioDef;
            unlockRequirement = unlock;
        }
#endif
    }
}
