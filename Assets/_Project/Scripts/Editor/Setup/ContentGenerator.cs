using System.Collections.Generic;
using RedlineLegends.AI;
using RedlineLegends.Audio;
using RedlineLegends.Career;
using RedlineLegends.Content;
using RedlineLegends.Core;
using RedlineLegends.Economy;
using RedlineLegends.Events;
using RedlineLegends.Progression;
using RedlineLegends.Tracks;
using RedlineLegends.Upgrades;
using RedlineLegends.Vehicles;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// Authors the initial content set as ScriptableObject assets. Table-driven so adding a car or
    /// event is a new row here (or a hand-made asset in the same folders). Re-running updates
    /// existing assets in place and keeps their GUIDs.
    /// </summary>
    public static class ContentGenerator
    {
        public const string ConfigPath = EditorPaths.Resources + "/" + GameConfig.ResourcePath + ".asset";
        public const string DatabasePath = EditorPaths.Content + "/ContentDatabase.asset";
        public const string ProgressionPath = EditorPaths.Content + "/ProgressionConfig.asset";
        public const string StarterVehicleId = "veh_street_kestrel";

        private sealed class VehicleRow
        {
            public string Id, Name, Brand;
            public VehicleClass Class;
            public int Price;
            public DrivetrainType Drive;
            public float Hp, Torque, Mass, TopKmh, Redline, Drag, Grip, Brake, Steer;
            public float[] Gears;
            public float FinalDrive;
            public float Turbo;
            public int UnlockLevel;
            public Color[] Paints;
        }

        // ---------------------------------------------------------------- vehicles
        private static readonly VehicleRow[] Vehicles =
        {
            new VehicleRow
            {
                Id = StarterVehicleId, Name = "Kestrel GT", Brand = "Aster", Class = VehicleClass.Street, Price = 12000,
                Drive = DrivetrainType.FWD, Hp = 150f, Torque = 205f, Mass = 1180f, TopKmh = 196f, Redline = 6800f,
                Drag = 0.31f, Grip = 1.0f, Brake = 5200f, Steer = 6.5f, Gears = new[] { 3.6f, 2.1f, 1.45f, 1.08f, 0.86f },
                FinalDrive = 4.1f, Turbo = 1f, UnlockLevel = 0,
                Paints = new[] { Hex("D8D9DB"), Hex("1F4FB5"), Hex("C6242E"), Hex("2A2A2E") }
            },
            new VehicleRow
            {
                Id = "veh_street_vulcan", Name = "Vulcan 240", Brand = "Norrad", Class = VehicleClass.Street, Price = 18500,
                Drive = DrivetrainType.RWD, Hp = 240f, Torque = 320f, Mass = 1350f, TopKmh = 236f, Redline = 7000f,
                Drag = 0.32f, Grip = 1.04f, Brake = 6000f, Steer = 6.8f, Gears = new[] { 3.4f, 2.05f, 1.45f, 1.1f, 0.87f, 0.72f },
                FinalDrive = 3.9f, Turbo = 1f, UnlockLevel = 0,
                Paints = new[] { Hex("F2B71B"), Hex("101012"), Hex("E8E8E8"), Hex("2E7D32") }
            },
            new VehicleRow
            {
                Id = "veh_sport_stratos", Name = "Stratos R", Brand = "Veloce", Class = VehicleClass.Sport, Price = 46000,
                Drive = DrivetrainType.AWD, Hp = 380f, Torque = 480f, Mass = 1480f, TopKmh = 276f, Redline = 7400f,
                Drag = 0.3f, Grip = 1.14f, Brake = 7800f, Steer = 7.6f, Gears = new[] { 3.3f, 2.2f, 1.6f, 1.25f, 1.0f, 0.82f },
                FinalDrive = 3.7f, Turbo = 1.12f, UnlockLevel = 4,
                Paints = new[] { Hex("1E88E5"), Hex("B71C1C"), Hex("FAFAFA"), Hex("212121") }
            },
        };

        // ---------------------------------------------------------------- AI profiles
        private struct AIRow
        {
            public string Id, Name;
            public AIDifficultyTier Tier;
            public float Reward, Reaction, Aggression, Cornering, Braking, Throttle, Mistakes, Speed;
            public int Stage;
            public float DragMin, DragMax, Launch, Shift, FalseStart;
            public DragNitrousStrategy Nitrous;
        }

        private static readonly AIRow[] AIProfiles =
        {
            new AIRow { Id = "ai_rookie", Name = "Rookie", Tier = AIDifficultyTier.Rookie, Reward = 1f, Reaction = 0.45f, Aggression = 0.15f, Cornering = 0.55f, Braking = 0.55f, Throttle = 0.6f, Mistakes = 2.5f, Speed = 0.78f, Stage = 0, DragMin = 0.35f, DragMax = 0.75f, Launch = 0.45f, Shift = 0.45f, FalseStart = 0.03f, Nitrous = DragNitrousStrategy.FinalStretch },
            new AIRow { Id = "ai_amateur", Name = "Amateur", Tier = AIDifficultyTier.Amateur, Reward = 1.15f, Reaction = 0.35f, Aggression = 0.3f, Cornering = 0.68f, Braking = 0.66f, Throttle = 0.72f, Mistakes = 1.6f, Speed = 0.85f, Stage = 1, DragMin = 0.28f, DragMax = 0.6f, Launch = 0.6f, Shift = 0.6f, FalseStart = 0.03f, Nitrous = DragNitrousStrategy.AfterSecondShift },
            new AIRow { Id = "ai_pro", Name = "Pro", Tier = AIDifficultyTier.Pro, Reward = 1.35f, Reaction = 0.26f, Aggression = 0.5f, Cornering = 0.8f, Braking = 0.78f, Throttle = 0.84f, Mistakes = 1.0f, Speed = 0.91f, Stage = 2, DragMin = 0.22f, DragMax = 0.45f, Launch = 0.75f, Shift = 0.75f, FalseStart = 0.02f, Nitrous = DragNitrousStrategy.AfterSecondShift },
            new AIRow { Id = "ai_expert", Name = "Expert", Tier = AIDifficultyTier.Expert, Reward = 1.6f, Reaction = 0.2f, Aggression = 0.65f, Cornering = 0.9f, Braking = 0.88f, Throttle = 0.92f, Mistakes = 0.5f, Speed = 0.96f, Stage = 3, DragMin = 0.18f, DragMax = 0.34f, Launch = 0.88f, Shift = 0.88f, FalseStart = 0.015f, Nitrous = DragNitrousStrategy.AtLaunch },
            new AIRow { Id = "ai_legend", Name = "Legend", Tier = AIDifficultyTier.Legend, Reward = 2f, Reaction = 0.15f, Aggression = 0.8f, Cornering = 0.97f, Braking = 0.96f, Throttle = 0.98f, Mistakes = 0.2f, Speed = 1f, Stage = 3, DragMin = 0.14f, DragMax = 0.26f, Launch = 0.96f, Shift = 0.96f, FalseStart = 0.01f, Nitrous = DragNitrousStrategy.Random },
        };

        // ---------------------------------------------------------------- generation
        public static GameConfig Generate()
        {
            EditorPaths.EnsureFolder(EditorPaths.Content + "/Vehicles");
            EditorPaths.EnsureFolder(EditorPaths.Content + "/Upgrades");
            EditorPaths.EnsureFolder(EditorPaths.Content + "/Tracks");
            EditorPaths.EnsureFolder(EditorPaths.Content + "/Events");
            EditorPaths.EnsureFolder(EditorPaths.Content + "/Championships");
            EditorPaths.EnsureFolder(EditorPaths.Content + "/AIProfiles");
            EditorPaths.EnsureFolder(EditorPaths.Content + "/Audio");
            EditorPaths.EnsureFolder(EditorPaths.Resources);

            var upgrades = GenerateUpgrades();
            var audio = EditorPaths.GetOrCreateAsset<VehicleAudioDefinition>(EditorPaths.Content + "/Audio/aud_generic.asset");
            audio.EditorInitialize("aud_generic");
            EditorUtility.SetDirty(audio);

            var vehicles = GenerateVehicles(upgrades, audio);
            var profiles = GenerateAIProfiles();
            var tracks = GenerateTracks();
            var events = GenerateEvents(tracks, profiles, vehicles);
            var championships = GenerateChampionships(events);

            var database = EditorPaths.GetOrCreateAsset<ContentDatabase>(DatabasePath);
            database.EditorSetContent(vehicles.ToArray(), Ordered(upgrades), Ordered(tracks),
                Ordered(events), championships.ToArray(), Ordered(profiles));
            EditorUtility.SetDirty(database);

            var progression = EditorPaths.GetOrCreateAsset<ProgressionConfig>(ProgressionPath);
            progression.EditorInitialize(12000, StarterVehicleId);
            EditorUtility.SetDirty(progression);

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(EditorPaths.InputActions);
            if (actions == null) Debug.LogError("[Setup] Input actions asset missing at " + EditorPaths.InputActions);

            var config = EditorPaths.GetOrCreateAsset<GameConfig>(ConfigPath);
            config.EditorInitialize(database, progression, actions);
            EditorUtility.SetDirty(config);

            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Content generated: " + vehicles.Count + " vehicles, " + events.Count + " events, " + championships.Count + " championships.");
            return config;
        }

        private static Dictionary<string, VehicleUpgradeDefinition> GenerateUpgrades()
        {
            var result = new Dictionary<string, VehicleUpgradeDefinition>();
            foreach (VehicleClass cls in System.Enum.GetValues(typeof(VehicleClass)))
            {
                float priceScale = cls == VehicleClass.Street ? 1f : cls == VehicleClass.Sport ? 2.2f : cls == VehicleClass.Super ? 4.5f : 8f;
                foreach (UpgradeCategory cat in System.Enum.GetValues(typeof(UpgradeCategory)))
                {
                    string id = "upg_" + cls.ToString().ToLowerInvariant() + "_" + cat.ToString().ToLowerInvariant();
                    var asset = EditorPaths.GetOrCreateAsset<VehicleUpgradeDefinition>(EditorPaths.Content + "/Upgrades/" + id + ".asset");
                    asset.EditorInitialize(id, cls + " " + Label(cat), cat, BuildStages(cat, priceScale));
                    EditorUtility.SetDirty(asset);
                    result[id] = asset;
                }
            }
            return result;
        }

        private static string Label(UpgradeCategory cat)
        {
            switch (cat)
            {
                case UpgradeCategory.ECU: return "ECU";
                case UpgradeCategory.WeightReduction: return "Weight Reduction";
                default: return cat.ToString();
            }
        }

        private static UpgradeStage[] BuildStages(UpgradeCategory cat, float priceScale)
        {
            int[] basePrice = { 2200, 4800, 9500 };
            int[] level = { 1, 3, 6 };
            var stages = new UpgradeStage[3];
            for (int s = 0; s < 3; s++)
            {
                stages[s] = new UpgradeStage
                {
                    DisplayName = "Stage " + (s + 1),
                    Price = Mathf.RoundToInt(basePrice[s] * priceScale / 100f) * 100,
                    RequiredPlayerLevel = level[s],
                    Modifiers = StageModifiers(cat, s)
                };
            }
            return stages;
        }

        /// <summary>Per-stage gains (cumulative when applied). Values are deliberately modest so upgrades stack sanely.</summary>
        private static StatModifier[] StageModifiers(UpgradeCategory cat, int s)
        {
            switch (cat)
            {
                case UpgradeCategory.Engine:
                    return new[] { M(VehicleStatId.PeakTorque, ModifierOp.Multiply, 1.08f), M(VehicleStatId.PeakPower, ModifierOp.Multiply, 1.08f), M(VehicleStatId.EngineInertia, ModifierOp.Multiply, 0.96f) };
                case UpgradeCategory.Turbo:
                    return new[] { M(VehicleStatId.TurboBoost, ModifierOp.Set, 1.12f + s * 0.1f), M(VehicleStatId.TurboSpool, ModifierOp.Set, 0.7f - s * 0.12f) };
                case UpgradeCategory.ECU:
                    return new[] { M(VehicleStatId.PeakTorque, ModifierOp.Multiply, 1.03f), M(VehicleStatId.PeakPower, ModifierOp.Multiply, 1.03f), M(VehicleStatId.Redline, ModifierOp.Add, 200f) };
                case UpgradeCategory.Transmission:
                    return new[] { M(VehicleStatId.ShiftTime, ModifierOp.Multiply, 0.82f), M(VehicleStatId.DrivelineEfficiency, ModifierOp.Multiply, 1.015f) };
                case UpgradeCategory.Tires:
                    return new[] { M(VehicleStatId.LateralGrip, ModifierOp.Multiply, 1.06f), M(VehicleStatId.LongitudinalGrip, ModifierOp.Multiply, 1.06f), M(VehicleStatId.SlideGripFraction, ModifierOp.Add, 0.02f) };
                case UpgradeCategory.Suspension:
                    return new[] { M(VehicleStatId.SpringRate, ModifierOp.Multiply, 1.1f), M(VehicleStatId.Damping, ModifierOp.Multiply, 1.08f), M(VehicleStatId.AntiRoll, ModifierOp.Multiply, 1.12f), M(VehicleStatId.SteerResponse, ModifierOp.Multiply, 1.05f) };
                case UpgradeCategory.Brakes:
                    return new[] { M(VehicleStatId.BrakeTorque, ModifierOp.Multiply, 1.12f), M(VehicleStatId.HandbrakeTorque, ModifierOp.Multiply, 1.08f) };
                case UpgradeCategory.WeightReduction:
                    return new[] { M(VehicleStatId.MassKg, ModifierOp.Multiply, 0.965f) };
                case UpgradeCategory.Nitrous:
                    return new[] { M(VehicleStatId.NitrousCapacity, ModifierOp.Set, 3f + s * 1.5f), M(VehicleStatId.NitrousPower, ModifierOp.Set, 1.22f + s * 0.07f) };
                default:
                    return new StatModifier[0];
            }
        }

        private static StatModifier M(VehicleStatId id, ModifierOp op, float value) => new StatModifier(id, op, value);

        private static List<VehicleDefinition> GenerateVehicles(Dictionary<string, VehicleUpgradeDefinition> upgrades, VehicleAudioDefinition audio)
        {
            var paint = MaterialFactory.CarPaint("Car_Paint", Hex("C6242E"));
            var glass = MaterialFactory.Glass("Car_Glass", new Color(0.05f, 0.08f, 0.1f, 0.55f));
            var tire = MaterialFactory.Opaque("Car_Tire", new Color(0.05f, 0.05f, 0.05f), 0f, 0.35f);
            var rim = MaterialFactory.Opaque("Car_Rim", new Color(0.75f, 0.76f, 0.78f), 0.9f, 0.7f);
            var trim = MaterialFactory.Opaque("Car_Trim", new Color(0.08f, 0.08f, 0.09f), 0.1f, 0.5f);
            var lightFront = MaterialFactory.Emissive("Car_LightFront", Color.white, new Color(1.5f, 1.5f, 1.4f));
            var lightRear = MaterialFactory.Emissive("Car_LightRear", new Color(0.6f, 0.05f, 0.05f), new Color(2.5f, 0.1f, 0.1f));

            var result = new List<VehicleDefinition>();
            foreach (var row in Vehicles)
            {
                var stats = new VehicleStats();
                stats.Engine.PeakPowerHp = row.Hp;
                stats.Engine.PeakTorqueNm = row.Torque;
                stats.Engine.RedlineRpm = row.Redline;
                stats.Engine.LimiterRpm = row.Redline + 250f;
                stats.Engine.TurboBoostMultiplier = row.Turbo;
                stats.Transmission.Drivetrain = row.Drive;
                stats.Transmission.GearRatios = row.Gears;
                stats.Transmission.FinalDrive = row.FinalDrive;
                stats.Chassis.MassKg = row.Mass;
                stats.Chassis.CenterOfMassOffset = new Vector3(0f,
                    row.Class == VehicleClass.Street ? 0.5f : row.Class == VehicleClass.Sport ? 0.45f : 0.4f, 0.02f);
                stats.Chassis.TopSpeedKmh = row.TopKmh;
                stats.Chassis.DragCoefficient = row.Drag;
                stats.Chassis.DownforceCoefficient = row.Class >= VehicleClass.Super ? 0.35f : 0f;
                stats.Tires.LateralGrip = row.Grip;
                stats.Tires.LongitudinalGrip = row.Grip * 1.05f;
                stats.Tires.WheelRadiusM = PlaceholderCarBuilder.ShapeFor(row.Class).WheelRadius;
                stats.Brakes.BrakeTorqueNm = row.Brake;
                stats.Brakes.HandbrakeTorqueNm = row.Brake * 0.7f;
                stats.Handling.SteerResponse = row.Steer;
                stats.Suspension.SpringRate = row.Mass * 28f;
                stats.Suspension.Damping = row.Mass * 3.1f;
                stats.Suspension.AntiRoll = row.Mass * 6.5f;
                stats.Suspension.RideHeightM = 0f;

                var slots = new List<UpgradeSlot>();
                foreach (UpgradeCategory cat in System.Enum.GetValues(typeof(UpgradeCategory)))
                {
                    string id = "upg_" + row.Class.ToString().ToLowerInvariant() + "_" + cat.ToString().ToLowerInvariant();
                    slots.Add(new UpgradeSlot { Category = cat, Definition = upgrades[id] });
                }

                var paints = new PaintOption[row.Paints.Length];
                for (int i = 0; i < paints.Length; i++)
                    paints[i] = new PaintOption { Name = "Paint " + (i + 1), Color = row.Paints[i], Metallic = 0.65f, Smoothness = 0.85f, Price = i == 0 ? 0 : 400 };

                var prefab = PlaceholderCarBuilder.BuildPrefab(row.Id, row.Class, paint, glass, tire, rim, trim, lightFront, lightRear);
                var unlock = new UnlockRequirement { PlayerLevel = row.UnlockLevel };

                var def = EditorPaths.GetOrCreateAsset<VehicleDefinition>(EditorPaths.Content + "/Vehicles/" + row.Id + ".asset");
                def.EditorInitialize(row.Id, row.Name, row.Brand, row.Class, row.Price, stats, slots.ToArray(), paints, prefab, audio, unlock);
                EditorUtility.SetDirty(def);
                result.Add(def);
            }
            return result;
        }

        private static Dictionary<string, AIProfile> GenerateAIProfiles()
        {
            var result = new Dictionary<string, AIProfile>();
            foreach (var row in AIProfiles)
            {
                var asset = EditorPaths.GetOrCreateAsset<AIProfile>(EditorPaths.Content + "/AIProfiles/" + row.Id + ".asset");
                asset.EditorInitialize(row.Id, row.Name, row.Tier, row.Reward, row.Reaction, row.Aggression, row.Cornering, row.Braking,
                    row.Throttle, row.Mistakes, row.Speed, row.Stage, row.DragMin, row.DragMax, row.Launch, row.Shift, row.Nitrous, row.FalseStart);
                EditorUtility.SetDirty(asset);
                result[row.Id] = asset;
            }
            return result;
        }

        public const string CircuitTrackId = "trk_sunset_loop";
        public const string DragTrackId = "trk_harbor_strip";
        public const string ProvingGroundTrackId = "trk_proving_ground";
        public const string CircuitSceneName = "Track_SunsetLoop";
        public const string DragSceneName = "Track_HarborStrip";
        public const string ProvingGroundSceneName = "Track_ProvingGround";

        private static Dictionary<string, TrackDefinition> GenerateTracks()
        {
            var result = new Dictionary<string, TrackDefinition>();
            var proving = EditorPaths.GetOrCreateAsset<TrackDefinition>(EditorPaths.Content + "/Tracks/" + ProvingGroundTrackId + ".asset");
            proving.EditorInitialize(ProvingGroundTrackId, "Proving Ground", ProvingGroundSceneName, TrackTheme.Industrial, 0f, false, false, 1);
            EditorUtility.SetDirty(proving);
            result[ProvingGroundTrackId] = proving;

            var circuit = EditorPaths.GetOrCreateAsset<TrackDefinition>(EditorPaths.Content + "/Tracks/" + CircuitTrackId + ".asset");
            circuit.EditorInitialize(CircuitTrackId, "Sunset Loop", CircuitSceneName, TrackTheme.Coast, 1650f, true, false, 8);
            EditorUtility.SetDirty(circuit);
            result[CircuitTrackId] = circuit;

            var strip = EditorPaths.GetOrCreateAsset<TrackDefinition>(EditorPaths.Content + "/Tracks/" + DragTrackId + ".asset");
            strip.EditorInitialize(DragTrackId, "Harbor Strip", DragSceneName, TrackTheme.Industrial, 1000f, false, true, 2);
            EditorUtility.SetDirty(strip);
            result[DragTrackId] = strip;
            return result;
        }

        private static Dictionary<string, RaceEventDefinition> GenerateEvents(Dictionary<string, TrackDefinition> tracks,
            Dictionary<string, AIProfile> ai, List<VehicleDefinition> vehicles)
        {
            var result = new Dictionary<string, RaceEventDefinition>();
            var loop = tracks[CircuitTrackId];
            var strip = tracks[DragTrackId];
            var streetOnly = new VehicleRestriction { AllowedClasses = new[] { VehicleClass.Street } };
            var any = new VehicleRestriction();

            result["evt_c01_e01"] = Circuit("evt_c01_e01", "Sunset Loop Sprint", "One lap to learn the coast road.",
                loop, CircuitEventType.Sprint, 1, 3, ai["ai_rookie"], streetOnly, 170, Rewards(900, 180), UnlockRequirement.None, TimeOfDay.Sunset);
            result["evt_c01_e02"] = Circuit("evt_c01_e02", "Sunset Loop Race", "Two laps against a full rookie grid.",
                loop, CircuitEventType.Circuit, 2, 5, ai["ai_rookie"], streetOnly, 180, Rewards(1400, 260), After("evt_c01_e01"), TimeOfDay.Sunset);
            result["evt_c01_e03"] = TimeAttack("evt_c01_e03", "Sunset Time Attack", "Beat the clock. Three stars under 1:25.",
                loop, 1, streetOnly, 190, Rewards(1100, 200, 85f, 92f, 105f), After("evt_c01_e02"));
            result["evt_c01_e04"] = Drag("evt_c01_e04", "Harbor Strip Quarter", "Your first quarter mile. Watch the lights.",
                strip, DragDistance.QuarterMile, ai["ai_rookie"], null, "Tomas", streetOnly, 180, Rewards(1200, 220), After("evt_c01_e02"), false);
            result["evt_c01_e05"] = Circuit("evt_c01_e05", "Coast Road Showdown", "Three laps, amateur field. Win to clear the championship.",
                loop, CircuitEventType.Circuit, 3, 5, ai["ai_amateur"], streetOnly, 210, Rewards(2200, 400), After("evt_c01_e04"), TimeOfDay.Day, true);

            return result;
        }

        private static List<ChampionshipDefinition> GenerateChampionships(Dictionary<string, RaceEventDefinition> events)
        {
            var list = new List<ChampionshipDefinition>();
            var c1 = EditorPaths.GetOrCreateAsset<ChampionshipDefinition>(EditorPaths.Content + "/Championships/chp_01_beginner_streets.asset");
            c1.EditorInitialize("chp_01_beginner_streets", "Beginner Streets", "Where every legend starts.", 1,
                new[] { events["evt_c01_e01"], events["evt_c01_e02"], events["evt_c01_e03"], events["evt_c01_e04"], events["evt_c01_e05"] },
                UnlockRequirement.None, 5000, 1000);
            EditorUtility.SetDirty(c1);
            list.Add(c1);
            return list;
        }

        // ---------------------------------------------------------------- helpers
        private static RaceEventDefinition Circuit(string id, string name, string desc, TrackDefinition track, CircuitEventType type,
            int laps, int aiCount, AIProfile profile, VehicleRestriction restriction, int pr, RewardTable rewards,
            UnlockRequirement unlock, TimeOfDay time, bool boss = false)
        {
            var evt = EditorPaths.GetOrCreateAsset<CircuitEventDefinition>(EditorPaths.Content + "/Events/" + id + ".asset");
            evt.EditorInitializeBase(id, name, desc, track, rewards, restriction, pr, WeatherType.Clear, time, unlock, profile, new VehicleDefinition[0], boss);
            evt.EditorInitializeCircuit(type, laps, aiCount);
            EditorUtility.SetDirty(evt);
            return evt;
        }

        private static RaceEventDefinition TimeAttack(string id, string name, string desc, TrackDefinition track, int laps,
            VehicleRestriction restriction, int pr, RewardTable rewards, UnlockRequirement unlock)
        {
            var evt = EditorPaths.GetOrCreateAsset<CircuitEventDefinition>(EditorPaths.Content + "/Events/" + id + ".asset");
            evt.EditorInitializeBase(id, name, desc, track, rewards, restriction, pr, WeatherType.Clear, TimeOfDay.Day, unlock, null, new VehicleDefinition[0], false);
            evt.EditorInitializeCircuit(CircuitEventType.TimeAttack, laps, 0);
            EditorUtility.SetDirty(evt);
            return evt;
        }

        private static RaceEventDefinition Drag(string id, string name, string desc, TrackDefinition track, DragDistance distance,
            AIProfile profile, VehicleDefinition opponentCar, string opponentName, VehicleRestriction restriction, int pr,
            RewardTable rewards, UnlockRequirement unlock, bool boss)
        {
            var evt = EditorPaths.GetOrCreateAsset<DragEventDefinition>(EditorPaths.Content + "/Events/" + id + ".asset");
            evt.EditorInitializeBase(id, name, desc, track, rewards, restriction, pr, WeatherType.Clear, TimeOfDay.Night, unlock, profile, new VehicleDefinition[0], boss);
            evt.EditorInitializeDrag(distance, opponentCar, opponentName, 0);
            EditorUtility.SetDirty(evt);
            return evt;
        }

        private static RewardTable Rewards(int firstCredits, int firstXp, params float[] timeAttackThresholds)
        {
            return new RewardTable
            {
                ByPosition = new[]
                {
                    new PositionReward(firstCredits, firstXp, 3),
                    new PositionReward(Mathf.RoundToInt(firstCredits * 0.7f), Mathf.RoundToInt(firstXp * 0.7f), 2),
                    new PositionReward(Mathf.RoundToInt(firstCredits * 0.5f), Mathf.RoundToInt(firstXp * 0.5f), 1),
                    new PositionReward(Mathf.RoundToInt(firstCredits * 0.3f), Mathf.RoundToInt(firstXp * 0.35f), 1),
                    new PositionReward(Mathf.RoundToInt(firstCredits * 0.2f), Mathf.RoundToInt(firstXp * 0.25f), 0),
                    new PositionReward(Mathf.RoundToInt(firstCredits * 0.15f), Mathf.RoundToInt(firstXp * 0.2f), 0),
                },
                CompletionCredits = Mathf.RoundToInt(firstCredits * 0.2f),
                CompletionXp = Mathf.RoundToInt(firstXp * 0.25f),
                TimeAttackStarThresholds = timeAttackThresholds
            };
        }

        private static UnlockRequirement After(string eventId) => new UnlockRequirement { RequiredEventId = eventId };

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var color);
            return color;
        }

        private static T[] Ordered<T>(Dictionary<string, T> map)
        {
            var list = new List<T>(map.Values);
            return list.ToArray();
        }
    }
}
