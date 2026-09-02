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
    /// Authors the content set as ScriptableObject assets from compact tables: 15 cars, upgrade kits
    /// per class, tracks from <see cref="TrackSpecs"/>, 10 championships × 5 events, a 12-round drag
    /// ladder, AI profiles and achievements. Re-running updates assets in place (GUIDs preserved).
    /// Recommended performance ratings are computed from the actual car roster, not typed in.
    /// </summary>
    public static class ContentGenerator
    {
        public const string ConfigPath = EditorPaths.Resources + "/" + GameConfig.ResourcePath + ".asset";
        public const string VfxLibraryPath = EditorPaths.Settings + "/VfxLibrary.asset";
        public const string DatabasePath = EditorPaths.Content + "/ContentDatabase.asset";
        public const string ProgressionPath = EditorPaths.Content + "/ProgressionConfig.asset";
        public const string StarterVehicleId = "veh_street_kestrel";

        public const string CircuitTrackId = "trk_sunset_loop";
        public const string DragTrackId = "trk_harbor_strip";
        public const string ProvingGroundTrackId = "trk_proving_ground";
        public const string CircuitSceneName = "Track_SunsetLoop";
        public const string DragSceneName = "Track_HarborStrip";
        public const string ProvingGroundSceneName = "Track_ProvingGround";

        // ---------------------------------------------------------------- vehicles
        private sealed class VehicleRow
        {
            public string Id, Name, Brand;
            public VehicleClass Class;
            public DrivetrainType Drive;
            public float Hp, Torque, Mass, TopKmh;
            public int Price;
            public int UnlockLevel;
            public string UnlockChampionship;
            public Color[] Paints;
        }

        private static readonly VehicleRow[] Vehicles =
        {
            new VehicleRow { Id = StarterVehicleId, Name = "Kestrel GT", Brand = "Aster", Class = VehicleClass.Street, Drive = DrivetrainType.FWD, Hp = 150, Torque = 205, Mass = 1180, TopKmh = 196, Price = 12000, Paints = P("D8D9DB", "1F4FB5", "C6242E", "2A2A2E") },
            new VehicleRow { Id = "veh_street_corsa_v", Name = "Corsa V", Brand = "Aster", Class = VehicleClass.Street, Drive = DrivetrainType.FWD, Hp = 185, Torque = 240, Mass = 1210, TopKmh = 215, Price = 16000, UnlockLevel = 2, Paints = P("F5F5F5", "2E7D32", "FF8F00", "1A1A1A") },
            new VehicleRow { Id = "veh_street_vulcan", Name = "Vulcan 240", Brand = "Norrad", Class = VehicleClass.Street, Drive = DrivetrainType.RWD, Hp = 240, Torque = 320, Mass = 1350, TopKmh = 236, Price = 18500, Paints = P("F2B71B", "101012", "E8E8E8", "2E7D32") },
            new VehicleRow { Id = "veh_street_ibex", Name = "Ibex Rally", Brand = "Toran", Class = VehicleClass.Street, Drive = DrivetrainType.AWD, Hp = 210, Torque = 300, Mass = 1320, TopKmh = 218, Price = 21000, UnlockLevel = 3, Paints = P("1565C0", "FFFFFF", "C62828", "37474F") },
            new VehicleRow { Id = "veh_sport_stratos", Name = "Stratos R", Brand = "Veloce", Class = VehicleClass.Sport, Drive = DrivetrainType.AWD, Hp = 380, Torque = 480, Mass = 1480, TopKmh = 276, Price = 46000, UnlockLevel = 4, Paints = P("1E88E5", "B71C1C", "FAFAFA", "212121") },
            new VehicleRow { Id = "veh_sport_harrier", Name = "Harrier Turbo", Brand = "Toran", Class = VehicleClass.Sport, Drive = DrivetrainType.AWD, Hp = 340, Torque = 450, Mass = 1420, TopKmh = 268, Price = 42000, UnlockLevel = 5, Paints = P("E65100", "263238", "F5F5F5", "1B5E20") },
            new VehicleRow { Id = "veh_sport_lyra", Name = "Lyra GT-S", Brand = "Aster", Class = VehicleClass.Sport, Drive = DrivetrainType.RWD, Hp = 365, Torque = 470, Mass = 1390, TopKmh = 282, Price = 52000, UnlockChampionship = "chp_03_desert_series", Paints = P("C62828", "FFF176", "ECEFF1", "212121") },
            new VehicleRow { Id = "veh_sport_meridian", Name = "Meridian S", Brand = "Norrad", Class = VehicleClass.Sport, Drive = DrivetrainType.RWD, Hp = 420, Torque = 540, Mass = 1560, TopKmh = 288, Price = 58000, UnlockChampionship = "chp_02_city_challenge", Paints = P("4527A0", "E0E0E0", "00897B", "212121") },
            new VehicleRow { Id = "veh_super_ardent", Name = "Ardent GT3", Brand = "Toran", Class = VehicleClass.Super, Drive = DrivetrainType.RWD, Hp = 560, Torque = 640, Mass = 1350, TopKmh = 312, Price = 150000, UnlockChampionship = "chp_06_industrial_open", Paints = P("FFFFFF", "D50000", "0091EA", "212121") },
            new VehicleRow { Id = "veh_super_viper_x", Name = "Viperone X", Brand = "Norrad", Class = VehicleClass.Super, Drive = DrivetrainType.RWD, Hp = 590, Torque = 760, Mass = 1520, TopKmh = 322, Price = 130000, UnlockLevel = 12, Paints = P("6A1B9A", "FFD600", "ECEFF1", "1A1A1A") },
            new VehicleRow { Id = "veh_super_tempest", Name = "Tempest", Brand = "Veloce", Class = VehicleClass.Super, Drive = DrivetrainType.RWD, Hp = 620, Torque = 720, Mass = 1450, TopKmh = 330, Price = 145000, UnlockChampionship = "chp_04_mountain_cup", Paints = P("F9A825", "212121", "0277BD", "F5F5F5") },
            new VehicleRow { Id = "veh_super_helion", Name = "Helion", Brand = "Kurai", Class = VehicleClass.Super, Drive = DrivetrainType.AWD, Hp = 680, Torque = 800, Mass = 1620, TopKmh = 335, Price = 165000, UnlockChampionship = "chp_05_night_racing", Paints = P("00ACC1", "F5F5F5", "AD1457", "212121") },
            new VehicleRow { Id = "veh_hyper_wraith", Name = "Wraith Evo", Brand = "Norrad", Class = VehicleClass.Hyper, Drive = DrivetrainType.RWD, Hp = 980, Torque = 1100, Mass = 1420, TopKmh = 372, Price = 380000, UnlockChampionship = "chp_09_summit_series", Paints = P("212121", "B0BEC5", "D50000", "FFEB3B") },
            new VehicleRow { Id = "veh_hyper_zenith", Name = "Zenith", Brand = "Veloce", Class = VehicleClass.Hyper, Drive = DrivetrainType.AWD, Hp = 1050, Torque = 1200, Mass = 1550, TopKmh = 380, Price = 420000, UnlockChampionship = "chp_07_coastal_gp", Paints = P("FAFAFA", "C62828", "1565C0", "37474F") },
            new VehicleRow { Id = "veh_hyper_solaris", Name = "Solaris", Brand = "Kurai", Class = VehicleClass.Hyper, Drive = DrivetrainType.AWD, Hp = 1200, Torque = 1350, Mass = 1680, TopKmh = 400, Price = 520000, UnlockChampionship = "chp_08_neon_nights", Paints = P("FF6F00", "212121", "E0F7FA", "4A148C") },
        };

        private static Color[] P(params string[] hex)
        {
            var colors = new Color[hex.Length];
            for (int i = 0; i < hex.Length; i++)
            {
                ColorUtility.TryParseHtmlString("#" + hex[i], out colors[i]);
            }
            return colors;
        }

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

        // ---------------------------------------------------------------- championships
        private sealed class ChampionshipRow
        {
            public string Id, Name, Description;
            public string TrackA, TrackB, TrackC;   // TrackB may be the drag strip
            public string Profile, BossProfile;
            public VehicleClass[] Classes;
            public int Tier;
        }

        private static readonly ChampionshipRow[] Championships =
        {
            new ChampionshipRow { Id = "chp_01_beginner_streets", Name = "Beginner Streets", Description = "Where every legend starts.", Tier = 1, TrackA = CircuitTrackId, TrackB = DragTrackId, Profile = "ai_rookie", BossProfile = "ai_amateur", Classes = new[] { VehicleClass.Street } },
            new ChampionshipRow { Id = "chp_02_city_challenge", Name = "City Challenge", Description = "Ninety-degree corners and no room for error.", Tier = 2, TrackA = "trk_city_circuit", TrackB = CircuitTrackId, Profile = "ai_amateur", BossProfile = "ai_amateur", Classes = new[] { VehicleClass.Street } },
            new ChampionshipRow { Id = "chp_03_desert_series", Name = "Desert Series", Description = "Long sweepers, high speeds, hot brakes.", Tier = 3, TrackA = "trk_dune_pass", TrackB = "trk_ridge_highway", Profile = "ai_amateur", BossProfile = "ai_pro", Classes = new[] { VehicleClass.Street, VehicleClass.Sport } },
            new ChampionshipRow { Id = "chp_04_mountain_cup", Name = "Mountain Cup", Description = "Climb the pass. Every metre is uphill.", Tier = 4, TrackA = "trk_alpine_climb", TrackB = "trk_dune_pass", Profile = "ai_pro", BossProfile = "ai_pro", Classes = new[] { VehicleClass.Sport } },
            new ChampionshipRow { Id = "chp_05_night_racing", Name = "Night Racing", Description = "Neon, fog and a half-mile strip under floodlights.", Tier = 5, TrackA = "trk_night_run", TrackB = DragTrackId, Profile = "ai_pro", BossProfile = "ai_expert", Classes = new[] { VehicleClass.Sport } },
            new ChampionshipRow { Id = "chp_06_industrial_open", Name = "Industrial Open", Description = "Containers, concrete and a technical yard.", Tier = 6, TrackA = "trk_cargo_yard", TrackB = "trk_city_circuit", Profile = "ai_pro", BossProfile = "ai_expert", Classes = new[] { VehicleClass.Sport, VehicleClass.Super } },
            new ChampionshipRow { Id = "chp_07_coastal_gp", Name = "Coastal GP", Description = "The coast road at supercar pace.", Tier = 7, TrackA = CircuitTrackId, TrackB = "trk_ridge_highway", Profile = "ai_expert", BossProfile = "ai_expert", Classes = new[] { VehicleClass.Super } },
            new ChampionshipRow { Id = "chp_08_neon_nights", Name = "Neon Nights", Description = "Night circuit and the drag strip, no excuses.", Tier = 8, TrackA = "trk_night_run", TrackB = DragTrackId, Profile = "ai_expert", BossProfile = "ai_legend", Classes = new[] { VehicleClass.Super } },
            new ChampionshipRow { Id = "chp_09_summit_series", Name = "Summit Series", Description = "From the climb to the grand circuit.", Tier = 9, TrackA = "trk_alpine_climb", TrackB = "trk_grand_circuit", Profile = "ai_expert", BossProfile = "ai_legend", Classes = new[] { VehicleClass.Super, VehicleClass.Hyper } },
            new ChampionshipRow { Id = "chp_10_legends_cup", Name = "Legends Cup", Description = "The best cars, the best drivers, the last word.", Tier = 10, TrackA = "trk_grand_circuit", TrackB = "trk_ridge_highway", TrackC = "trk_night_run", Profile = "ai_legend", BossProfile = "ai_legend", Classes = new[] { VehicleClass.Hyper } },
        };

        private static readonly string[] RivalNames = { "Tomas", "Lena", "Rico", "Vale", "Kenji", "Marta", "Dez", "Noor", "Ivo", "Sasha", "Bo", "Zara" };

        // ---------------------------------------------------------------- generation
        public static GameConfig Generate()
        {
            foreach (var folder in new[] { "Vehicles", "Upgrades", "Tracks", "Events", "Championships", "AIProfiles", "Audio", "Achievements" })
                EditorPaths.EnsureFolder(EditorPaths.Content + "/" + folder);
            EditorPaths.EnsureFolder(EditorPaths.Resources);

            var upgrades = GenerateUpgrades();
            var audio = EditorPaths.GetOrCreateAsset<VehicleAudioDefinition>(EditorPaths.Content + "/Audio/aud_generic.asset");
            audio.EditorInitialize("aud_generic");
            EditorUtility.SetDirty(audio);

            var vehicles = GenerateVehicles(upgrades, audio);
            var profiles = GenerateAIProfiles();
            var tracks = GenerateTracks();
            var events = new Dictionary<string, RaceEventDefinition>();
            var championships = GenerateChampionships(tracks, profiles, vehicles, events);
            championships.Add(GenerateDragLadder(tracks, profiles, vehicles, events));
            var achievements = GenerateAchievements();

            var database = EditorPaths.GetOrCreateAsset<ContentDatabase>(DatabasePath);
            database.EditorSetContent(vehicles.ToArray(), Ordered(upgrades), Ordered(tracks), Ordered(events), championships.ToArray(),
                Ordered(profiles), achievements.ToArray());
            EditorUtility.SetDirty(database);

            var progression = EditorPaths.GetOrCreateAsset<ProgressionConfig>(ProgressionPath);
            progression.EditorInitialize(12000, StarterVehicleId);
            EditorUtility.SetDirty(progression);

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(EditorPaths.InputActions);
            if (actions == null) Debug.LogError("[Setup] Input actions asset missing at " + EditorPaths.InputActions);

            var vfx = GenerateVfxLibrary();
            var config = EditorPaths.GetOrCreateAsset<GameConfig>(ConfigPath);
            config.EditorInitialize(database, progression, actions, vfx);
            EditorUtility.SetDirty(config);

            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Content generated: " + vehicles.Count + " vehicles, " + tracks.Count + " tracks, " + events.Count + " events, "
                      + championships.Count + " championships, " + achievements.Count + " achievements.");
            return config;
        }

        // ---------------------------------------------------------------- upgrades
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

        // ---------------------------------------------------------------- vehicles
        private static List<VehicleDefinition> GenerateVehicles(Dictionary<string, VehicleUpgradeDefinition> upgrades, VehicleAudioDefinition audio)
        {
            var paint = MaterialFactory.CarPaint("Car_Paint", new Color(0.78f, 0.14f, 0.18f));
            var glass = MaterialFactory.Glass("Car_Glass", new Color(0.05f, 0.08f, 0.1f, 0.55f));
            var tire = MaterialFactory.Opaque("Car_Tire", new Color(0.05f, 0.05f, 0.05f), 0f, 0.35f);
            var rim = MaterialFactory.Opaque("Car_Rim", new Color(0.75f, 0.76f, 0.78f), 0.9f, 0.7f);
            var trim = MaterialFactory.Opaque("Car_Trim", new Color(0.08f, 0.08f, 0.09f), 0.1f, 0.5f);
            var lightFront = MaterialFactory.Emissive("Car_LightFront", Color.white, new Color(1.5f, 1.5f, 1.4f));
            var lightRear = MaterialFactory.Emissive("Car_LightRear", new Color(0.6f, 0.05f, 0.05f), new Color(2.5f, 0.1f, 0.1f));

            var result = new List<VehicleDefinition>();
            foreach (var row in Vehicles)
            {
                var stats = BuildStats(row);
                var slots = new List<UpgradeSlot>();
                foreach (UpgradeCategory cat in System.Enum.GetValues(typeof(UpgradeCategory)))
                    slots.Add(new UpgradeSlot { Category = cat, Definition = upgrades["upg_" + row.Class.ToString().ToLowerInvariant() + "_" + cat.ToString().ToLowerInvariant()] });

                var paints = new PaintOption[row.Paints.Length];
                for (int i = 0; i < paints.Length; i++)
                    paints[i] = new PaintOption { Name = "Paint " + (i + 1), Color = row.Paints[i], Metallic = 0.65f, Smoothness = 0.85f, Price = i == 0 ? 0 : 400 * (1 + (int)row.Class) };

                var prefab = PlaceholderCarBuilder.BuildPrefab(row.Id, row.Class, paint, glass, tire, rim, trim, lightFront, lightRear);
                var unlock = new UnlockRequirement { PlayerLevel = row.UnlockLevel, RequiredChampionshipId = row.UnlockChampionship ?? "" };

                var def = EditorPaths.GetOrCreateAsset<VehicleDefinition>(EditorPaths.Content + "/Vehicles/" + row.Id + ".asset");
                def.EditorInitialize(row.Id, row.Name, row.Brand, row.Class, row.Price, stats, slots.ToArray(), paints, prefab, audio, unlock);
                EditorUtility.SetDirty(def);
                result.Add(def);
            }
            return result;
        }

        /// <summary>Physical stats derived per class so the roster stays coherent when a number changes.</summary>
        private static VehicleStats BuildStats(VehicleRow row)
        {
            var s = new VehicleStats();
            int cls = (int)row.Class;
            s.Engine.PeakPowerHp = row.Hp;
            s.Engine.PeakTorqueNm = row.Torque;
            s.Engine.RedlineRpm = new[] { 6800f, 7400f, 8200f, 8800f }[cls];
            s.Engine.LimiterRpm = s.Engine.RedlineRpm + 250f;
            s.Engine.EngineInertia = new[] { 0.22f, 0.18f, 0.15f, 0.12f }[cls];
            s.Engine.TurboBoostMultiplier = row.Class >= VehicleClass.Super ? 1.1f : 1f;
            s.Transmission.Drivetrain = row.Drive;
            s.Transmission.GearRatios = cls switch
            {
                0 => new[] { 3.6f, 2.1f, 1.45f, 1.08f, 0.86f },
                1 => new[] { 3.3f, 2.2f, 1.6f, 1.25f, 1.0f, 0.82f },
                2 => new[] { 3.2f, 2.15f, 1.6f, 1.28f, 1.05f, 0.88f, 0.74f },
                _ => new[] { 3.1f, 2.1f, 1.58f, 1.26f, 1.04f, 0.87f, 0.72f },
            };
            s.Transmission.FinalDrive = new[] { 4.1f, 3.7f, 3.4f, 3.2f }[cls];
            s.Transmission.ShiftTimeSeconds = new[] { 0.24f, 0.18f, 0.12f, 0.08f }[cls];
            s.Chassis.MassKg = row.Mass;
            s.Chassis.CenterOfMassOffset = new Vector3(0f, new[] { 0.5f, 0.45f, 0.4f, 0.38f }[cls], 0.02f);
            s.Chassis.TopSpeedKmh = row.TopKmh;
            s.Chassis.DragCoefficient = new[] { 0.31f, 0.3f, 0.32f, 0.33f }[cls];
            s.Chassis.FrontalAreaM2 = new[] { 2.2f, 2.1f, 2.0f, 1.95f }[cls];
            s.Chassis.DownforceCoefficient = new[] { 0f, 0.1f, 0.45f, 0.8f }[cls];
            float grip = new[] { 1.02f, 1.15f, 1.35f, 1.5f }[cls] + (row.Drive == DrivetrainType.AWD ? 0.02f : 0f);
            s.Tires.LateralGrip = grip;
            s.Tires.LongitudinalGrip = grip * 1.05f;
            s.Tires.WheelRadiusM = PlaceholderCarBuilder.ShapeFor(row.Class).WheelRadius;
            s.Handling.MaxSteerAngleDeg = new[] { 32f, 30f, 28f, 26f }[cls];
            s.Handling.SteerResponse = new[] { 6.5f, 7.5f, 8.5f, 9.5f }[cls];
            s.Handling.StabilityAssist = new[] { 0.35f, 0.3f, 0.25f, 0.2f }[cls];
            s.Brakes.BrakeTorqueNm = row.Mass * new[] { 4.4f, 5.2f, 6.2f, 7f }[cls];
            s.Brakes.HandbrakeTorqueNm = s.Brakes.BrakeTorqueNm * 0.7f;
            s.Suspension.SpringRate = row.Mass * new[] { 28f, 34f, 42f, 48f }[cls];
            s.Suspension.Damping = row.Mass * new[] { 3.1f, 3.6f, 4.2f, 4.6f }[cls];
            s.Suspension.AntiRoll = row.Mass * new[] { 6.5f, 8f, 10f, 12f }[cls];
            s.Suspension.RideHeightM = 0f;
            s.Nitrous.CapacitySeconds = 0f;
            return s;
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

        // ---------------------------------------------------------------- tracks
        private static Dictionary<string, TrackDefinition> GenerateTracks()
        {
            var result = new Dictionary<string, TrackDefinition>();
            var proving = EditorPaths.GetOrCreateAsset<TrackDefinition>(EditorPaths.Content + "/Tracks/" + ProvingGroundTrackId + ".asset");
            proving.EditorInitialize(ProvingGroundTrackId, "Proving Ground", ProvingGroundSceneName, TrackTheme.Industrial, 0f, false, false, 1);
            EditorUtility.SetDirty(proving);
            result[ProvingGroundTrackId] = proving;

            foreach (var spec in TrackSpecs.All)
            {
                var track = EditorPaths.GetOrCreateAsset<TrackDefinition>(EditorPaths.Content + "/Tracks/" + spec.Id + ".asset");
                track.EditorInitialize(spec.Id, spec.DisplayName, spec.SceneName, spec.Theme, spec.LengthEstimate, spec.Loop, false, spec.GridSlots);
                EditorUtility.SetDirty(track);
                result[spec.Id] = track;
            }

            var strip = EditorPaths.GetOrCreateAsset<TrackDefinition>(EditorPaths.Content + "/Tracks/" + DragTrackId + ".asset");
            strip.EditorInitialize(DragTrackId, "Harbor Strip", DragSceneName, TrackTheme.DragStrip, 1000f, false, true, 2);
            EditorUtility.SetDirty(strip);
            result[DragTrackId] = strip;
            return result;
        }

        // ---------------------------------------------------------------- championships & events
        private static List<ChampionshipDefinition> GenerateChampionships(Dictionary<string, TrackDefinition> tracks,
            Dictionary<string, AIProfile> profiles, List<VehicleDefinition> vehicles, Dictionary<string, RaceEventDefinition> events)
        {
            var list = new List<ChampionshipDefinition>();
            for (int c = 0; c < Championships.Length; c++)
            {
                var row = Championships[c];
                int n = c + 1;
                var profile = profiles[row.Profile];
                var boss = profiles[row.BossProfile];
                var restriction = new VehicleRestriction { AllowedClasses = row.Classes };
                int pr = RecommendedRating(vehicles, row.Classes, profile.VehicleUpgradeStage);
                int bossPr = RecommendedRating(vehicles, row.Classes, boss.VehicleUpgradeStage);
                float scale = Mathf.Pow(1.35f, c);
                int credits = Mathf.RoundToInt(900f * scale / 50f) * 50;
                int xp = Mathf.RoundToInt(180f * Mathf.Pow(1.3f, c));
                var trackA = tracks[row.TrackA];
                var trackB = tracks[row.TrackB];
                bool dragB = trackB.SupportsDrag;
                var circuitB = dragB ? trackA : trackB;
                var time = n >= 5 && n != 7 ? TimeOfDay.Night : n % 2 == 0 ? TimeOfDay.Sunset : TimeOfDay.Day;
                if (trackA.Theme == TrackTheme.NightCity) time = TimeOfDay.Night;
                var weather = n == 6 || n == 9 ? WeatherType.Overcast : WeatherType.Clear;

                string prefix = "evt_c" + n.ToString("00") + "_e";
                var chain = new List<RaceEventDefinition>();
                UnlockRequirement first = c == 0 ? UnlockRequirement.None
                    : new UnlockRequirement { RequiredChampionshipId = Championships[c - 1].Id, RequiredChampionshipStars = 8 };

                // E1 sprint / point-to-point on A
                chain.Add(Circuit(events, prefix + "01", trackA.DisplayName + " Sprint", "One flying lap to open the " + row.Name + ".",
                    trackA, CircuitEventType.Sprint, 1, 3, profile, restriction, pr, Rewards(credits, xp), first, time, weather));
                // E2 race on A
                chain.Add(Circuit(events, prefix + "02", trackA.DisplayName + " Race", "Full grid, " + (n >= 5 ? 3 : 2) + " laps.",
                    trackA, CircuitEventType.Circuit, trackA.IsLoop ? (n >= 5 ? 3 : 2) : 1, 5, profile, restriction, pr,
                    Rewards(Mathf.RoundToInt(credits * 1.3f), Mathf.RoundToInt(xp * 1.3f)), After(prefix + "01"), time, weather));
                // E3 special on B (or A when B is the strip)
                float lapEstimate = LapTimeEstimate(circuitB, profile);
                switch (n % 3)
                {
                    case 1:
                        chain.Add(TimeAttack(events, prefix + "03", circuitB.DisplayName + " Time Attack", "Alone against the clock.",
                            circuitB, 1, restriction, pr, Rewards(credits, xp, lapEstimate * 1.0f, lapEstimate * 1.08f, lapEstimate * 1.2f), After(prefix + "02"), time, weather));
                        break;
                    case 2:
                        chain.Add(Circuit(events, prefix + "03", circuitB.DisplayName + " Elimination", "Last place is out every 20 seconds.",
                            circuitB, CircuitEventType.Elimination, circuitB.IsLoop ? 3 : 1, 5, profile, restriction, pr,
                            Rewards(Mathf.RoundToInt(credits * 1.2f), Mathf.RoundToInt(xp * 1.2f)), After(prefix + "02"), time, weather, false, 20f));
                        break;
                    default:
                        chain.Add(Circuit(events, prefix + "03", circuitB.DisplayName + " Checkpoint", "Beat the countdown gate by gate.",
                            circuitB, CircuitEventType.Checkpoint, circuitB.IsLoop ? 2 : 1, 3, profile, restriction, pr,
                            Rewards(Mathf.RoundToInt(credits * 1.2f), Mathf.RoundToInt(xp * 1.2f)), After(prefix + "02"), time, weather, false, 20f,
                            Mathf.Max(20f, lapEstimate * 0.12f), Mathf.Max(6f, lapEstimate * 0.1f)));
                        break;
                }
                // E4 drag or second circuit race
                if (dragB)
                {
                    var rival = PickRival(vehicles, row.Classes, c);
                    chain.Add(Drag(events, prefix + "04", "Harbor Strip " + (n >= 5 ? "Half" : "Quarter"), "Reaction, shifts, nerve.",
                        trackB, n >= 5 ? DragDistance.HalfMile : DragDistance.QuarterMile, profile, rival, RivalNames[c % RivalNames.Length], restriction, pr,
                        Rewards(Mathf.RoundToInt(credits * 1.2f), Mathf.RoundToInt(xp * 1.2f)), After(prefix + "03"), false));
                }
                else
                {
                    chain.Add(Circuit(events, prefix + "04", trackB.DisplayName + " Race", "The second track of the series.",
                        trackB, CircuitEventType.Circuit, trackB.IsLoop ? 3 : 1, 5, profile, restriction, pr,
                        Rewards(Mathf.RoundToInt(credits * 1.4f), Mathf.RoundToInt(xp * 1.4f)), After(prefix + "03"), time, weather));
                }
                // E5 boss
                var bossTrack = row.TrackC != null ? tracks[row.TrackC] : trackA;
                chain.Add(Circuit(events, prefix + "05", row.Name + " Final", "Beat the champion to clear the series.",
                    bossTrack, CircuitEventType.Circuit, bossTrack.IsLoop ? (n >= 7 ? 4 : 3) : 1, 5, boss, restriction, bossPr,
                    Rewards(Mathf.RoundToInt(credits * 2.2f), Mathf.RoundToInt(xp * 2f)), After(prefix + "04"), time, weather, true));

                var championship = EditorPaths.GetOrCreateAsset<ChampionshipDefinition>(EditorPaths.Content + "/Championships/" + row.Id + ".asset");
                championship.EditorInitialize(row.Id, row.Name, row.Description, row.Tier, chain.ToArray(), first,
                    Mathf.RoundToInt(5000f * scale / 100f) * 100, Mathf.RoundToInt(1000f * Mathf.Pow(1.3f, c)));
                EditorUtility.SetDirty(championship);
                list.Add(championship);
            }
            return list;
        }

        private static ChampionshipDefinition GenerateDragLadder(Dictionary<string, TrackDefinition> tracks, Dictionary<string, AIProfile> profiles,
            List<VehicleDefinition> vehicles, Dictionary<string, RaceEventDefinition> events)
        {
            var strip = tracks[DragTrackId];
            var chain = new List<RaceEventDefinition>();
            string[] profileByRound = { "ai_rookie", "ai_rookie", "ai_amateur", "ai_amateur", "ai_pro", "ai_pro", "ai_pro", "ai_pro", "ai_expert", "ai_expert", "ai_expert", "ai_legend" };
            VehicleClass[] classByRound = { VehicleClass.Street, VehicleClass.Street, VehicleClass.Street, VehicleClass.Street, VehicleClass.Sport, VehicleClass.Sport, VehicleClass.Sport, VehicleClass.Sport, VehicleClass.Super, VehicleClass.Super, VehicleClass.Super, VehicleClass.Hyper };
            for (int r = 0; r < 12; r++)
            {
                int round = r + 1;
                var profile = profiles[profileByRound[r]];
                var rival = PickRival(vehicles, new[] { classByRound[r] }, r);
                int rivalPr = VehicleSpecBuilder.BuildAtUniformStage(rival, profile.VehicleUpgradeStage).PerformanceRating;
                var restriction = new VehicleRestriction { MinPerformanceRating = Mathf.RoundToInt(rivalPr * 0.7f / 10f) * 10 };
                bool boss = round % 4 == 0;
                float scale = Mathf.Pow(1.3f, r);
                string id = "evt_drag_r" + round.ToString("00");
                chain.Add(Drag(events, id, "Round " + round + (boss ? " - Boss" : ""), boss ? "Beat the ladder boss." : "Ladder round " + round + ".",
                    strip, round <= 8 ? DragDistance.QuarterMile : DragDistance.HalfMile, profile, rival, RivalNames[r], restriction, rivalPr,
                    Rewards(Mathf.RoundToInt(800f * scale / 50f) * 50, Mathf.RoundToInt(160f * scale)), r == 0 ? UnlockRequirement.None : After("evt_drag_r" + r.ToString("00")), boss, round));
            }
            var ladder = EditorPaths.GetOrCreateAsset<ChampionshipDefinition>(EditorPaths.Content + "/Championships/chp_drag_ladder.asset");
            ladder.EditorInitialize("chp_drag_ladder", "Drag Tournament", "Twelve rounds of the strip. Faster cars, faster rivals.", 0, chain.ToArray(),
                UnlockRequirement.None, 25000, 4000);
            EditorUtility.SetDirty(ladder);
            return ladder;
        }

        private static int RecommendedRating(List<VehicleDefinition> vehicles, VehicleClass[] classes, int stage)
        {
            var ratings = new List<int>();
            foreach (var v in vehicles)
                if (System.Array.IndexOf(classes, v.VehicleClass) >= 0)
                    ratings.Add(VehicleSpecBuilder.BuildAtUniformStage(v, stage).PerformanceRating);
            if (ratings.Count == 0) return 250;
            ratings.Sort();
            return ratings[ratings.Count / 2];
        }

        private static VehicleDefinition PickRival(List<VehicleDefinition> vehicles, VehicleClass[] classes, int salt)
        {
            var pool = new List<VehicleDefinition>();
            foreach (var v in vehicles)
                if (System.Array.IndexOf(classes, v.VehicleClass) >= 0 && v.Id != StarterVehicleId) pool.Add(v);
            if (pool.Count == 0) pool.AddRange(vehicles);
            return pool[salt % pool.Count];
        }

        /// <summary>Rough lap time from length and the tier's typical average speed; only star thresholds use it.</summary>
        private static float LapTimeEstimate(TrackDefinition track, AIProfile profile)
        {
            float avg = profile.Tier switch
            {
                AIDifficultyTier.Rookie => 21f,
                AIDifficultyTier.Amateur => 25f,
                AIDifficultyTier.Pro => 30f,
                AIDifficultyTier.Expert => 35f,
                _ => 40f,
            };
            return Mathf.Max(30f, track.LengthMeters / avg);
        }

        private static RaceEventDefinition Circuit(Dictionary<string, RaceEventDefinition> events, string id, string name, string desc,
            TrackDefinition track, CircuitEventType type, int laps, int aiCount, AIProfile profile, VehicleRestriction restriction, int pr,
            RewardTable rewards, UnlockRequirement unlock, TimeOfDay time, WeatherType weather, bool boss = false,
            float eliminationInterval = 20f, float checkpointStart = 30f, float checkpointBonus = 8f)
        {
            var evt = EditorPaths.GetOrCreateAsset<CircuitEventDefinition>(EditorPaths.Content + "/Events/" + id + ".asset");
            evt.EditorInitializeBase(id, name, desc, track, rewards, restriction, pr, weather, time, unlock, profile, new VehicleDefinition[0], boss);
            evt.EditorInitializeCircuit(type, laps, aiCount, eliminationInterval, checkpointStart, checkpointBonus);
            EditorUtility.SetDirty(evt);
            events[id] = evt;
            return evt;
        }

        private static RaceEventDefinition TimeAttack(Dictionary<string, RaceEventDefinition> events, string id, string name, string desc,
            TrackDefinition track, int laps, VehicleRestriction restriction, int pr, RewardTable rewards, UnlockRequirement unlock, TimeOfDay time, WeatherType weather)
        {
            var evt = EditorPaths.GetOrCreateAsset<CircuitEventDefinition>(EditorPaths.Content + "/Events/" + id + ".asset");
            evt.EditorInitializeBase(id, name, desc, track, rewards, restriction, pr, weather, time, unlock, null, new VehicleDefinition[0], false);
            evt.EditorInitializeCircuit(CircuitEventType.TimeAttack, laps, 0);
            EditorUtility.SetDirty(evt);
            events[id] = evt;
            return evt;
        }

        private static RaceEventDefinition Drag(Dictionary<string, RaceEventDefinition> events, string id, string name, string desc,
            TrackDefinition track, DragDistance distance, AIProfile profile, VehicleDefinition opponentCar, string opponentName,
            VehicleRestriction restriction, int pr, RewardTable rewards, UnlockRequirement unlock, bool boss, int round = 0)
        {
            var evt = EditorPaths.GetOrCreateAsset<DragEventDefinition>(EditorPaths.Content + "/Events/" + id + ".asset");
            evt.EditorInitializeBase(id, name, desc, track, rewards, restriction, pr, WeatherType.Clear, TimeOfDay.Night, unlock, profile, new VehicleDefinition[0], boss);
            evt.EditorInitializeDrag(distance, opponentCar, opponentName, round);
            EditorUtility.SetDirty(evt);
            events[id] = evt;
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

        // ---------------------------------------------------------------- vfx & achievements
        private static VfxLibrary GenerateVfxLibrary()
        {
            var soft = VfxTextures.GetOrCreateSoftCircle();
            var streak = VfxTextures.GetOrCreateStreak();
            var smoke = MaterialFactory.Particle("Vfx_Smoke", soft, new Color(1f, 1f, 1f, 0.5f), additive: false);
            var sparks = MaterialFactory.Particle("Vfx_Sparks", soft, new Color(1f, 0.8f, 0.4f, 1f), additive: true);
            var nitrous = MaterialFactory.Particle("Vfx_Nitrous", soft, new Color(0.5f, 0.7f, 1f, 1f), additive: true);
            var skid = MaterialFactory.Particle("Vfx_Skid", streak, new Color(0.05f, 0.05f, 0.05f, 1f), additive: false, vertexColor: true);
            var library = EditorPaths.GetOrCreateAsset<VfxLibrary>(VfxLibraryPath);
            library.EditorInitialize(smoke, sparks, nitrous, skid);
            EditorUtility.SetDirty(library);
            return library;
        }

        private static List<AchievementDefinition> GenerateAchievements()
        {
            var rows = new (string id, string name, string desc, AchievementStat stat, int target, int credits, int xp)[]
            {
                ("ach_first_race", "Green Flag", "Finish your first race.", AchievementStat.RacesEntered, 1, 500, 100),
                ("ach_first_win", "Top Step", "Win a race.", AchievementStat.RacesWon, 1, 1000, 200),
                ("ach_ten_wins", "Serial Winner", "Win 10 races.", AchievementStat.RacesWon, 10, 4000, 600),
                ("ach_fifty_wins", "Dominant", "Win 50 races.", AchievementStat.RacesWon, 50, 15000, 2000),
                ("ach_drag_win", "Green Light Go", "Win a drag race.", AchievementStat.DragWins, 1, 800, 150),
                ("ach_drag_ten", "Strip King", "Win 10 drag races.", AchievementStat.DragWins, 10, 5000, 700),
                ("ach_perfect_shifts", "Redline Reflexes", "Land 25 perfect shifts.", AchievementStat.PerfectShifts, 25, 2000, 300),
                ("ach_top_speed_250", "Two-Fifty", "Reach 250 km/h.", AchievementStat.TopSpeedKmh, 250, 1500, 250),
                ("ach_top_speed_350", "Three-Fifty", "Reach 350 km/h.", AchievementStat.TopSpeedKmh, 350, 6000, 800),
                ("ach_collector_3", "Collector", "Own 3 cars.", AchievementStat.CarsOwned, 3, 3000, 400),
                ("ach_collector_10", "Showroom", "Own 10 cars.", AchievementStat.CarsOwned, 10, 20000, 2500),
                ("ach_stars_15", "Rising Star", "Earn 15 stars.", AchievementStat.TotalStars, 15, 2500, 400),
                ("ach_stars_100", "Constellation", "Earn 100 stars.", AchievementStat.TotalStars, 100, 25000, 3000),
                ("ach_championship", "Champion", "Complete a championship.", AchievementStat.ChampionshipsCompleted, 1, 5000, 800),
                ("ach_all_championships", "Legend", "Complete all ten championships.", AchievementStat.ChampionshipsCompleted, 10, 100000, 10000),
                ("ach_tuner", "Tuner", "Install 10 upgrades.", AchievementStat.UpgradesInstalled, 10, 2000, 300),
                ("ach_credits_100k", "Money Maker", "Earn 100,000 credits.", AchievementStat.CreditsEarned, 100000, 5000, 500),
            };
            var list = new List<AchievementDefinition>();
            foreach (var row in rows)
            {
                var asset = EditorPaths.GetOrCreateAsset<AchievementDefinition>(EditorPaths.Content + "/Achievements/" + row.id + ".asset");
                asset.EditorInitialize(row.id, row.name, row.desc, row.stat, row.target, row.credits, row.xp);
                EditorUtility.SetDirty(asset);
                list.Add(asset);
            }
            return list;
        }

        private static T[] Ordered<T>(Dictionary<string, T> map)
        {
            var list = new List<T>(map.Values);
            return list.ToArray();
        }
    }
}
