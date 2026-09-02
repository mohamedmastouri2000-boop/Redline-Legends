using RedlineLegends.Tuning;
using RedlineLegends.Upgrades;

namespace RedlineLegends.Vehicles
{
    /// <summary>
    /// Turns a definition plus the player's owned state into the resolved spec the simulation uses.
    /// Base stats are cloned, never modified, so the ScriptableObject stays pristine.
    /// </summary>
    public static class VehicleSpecBuilder
    {
        /// <summary>Stock car, no upgrades, default tuning.</summary>
        public static VehicleSpec BuildStock(VehicleDefinition definition)
            => Build(definition, null, null);

        /// <summary>
        /// upgradeStages: per category installed stage (0 = stock). Null means stock.
        /// tuning: null means default setup.
        /// </summary>
        public static VehicleSpec Build(VehicleDefinition definition, int[] upgradeStages, VehicleTuningData tuning)
        {
            var stats = definition.BaseStats.Clone();

            if (upgradeStages != null)
            {
                var slots = definition.UpgradeSlots;
                for (int i = 0; i < slots.Length; i++)
                {
                    var slot = slots[i];
                    if (slot.Definition == null) continue;
                    int cat = (int)slot.Category;
                    if (cat < 0 || cat >= upgradeStages.Length) continue;
                    slot.Definition.ApplyTo(stats, upgradeStages[cat]);
                }
            }

            if (tuning != null)
                VehicleTuningApplier.Apply(stats, tuning, definition.TuningLimits);

            return new VehicleSpec
            {
                VehicleId = definition.Id,
                Stats = stats,
                PerformanceRating = PerformanceRatingCalculator.Compute(stats)
            };
        }

        /// <summary>Uniform upgrade level in every category the car supports (used for AI opponents).</summary>
        public static VehicleSpec BuildAtUniformStage(VehicleDefinition definition, int stage)
        {
            int count = System.Enum.GetValues(typeof(UpgradeCategory)).Length;
            var stages = new int[count];
            for (int i = 0; i < count; i++) stages[i] = stage;
            return Build(definition, stages, null);
        }
    }
}
