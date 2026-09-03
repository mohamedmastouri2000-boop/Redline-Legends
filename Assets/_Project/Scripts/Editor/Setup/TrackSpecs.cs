using RedlineLegends.Tracks;
using UnityEngine;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// Everything that distinguishes one generated circuit from another: layout, width, theme
    /// colours, lighting and dressing style. Adding a track is adding a spec here.
    /// </summary>
    public sealed class CircuitSpec
    {
        public string Id;
        public string SceneName;
        public string DisplayName;
        public TrackTheme Theme;
        public string Dressing = "circuit";
        public bool Loop = true;
        public float HalfWidth = 6.5f;
        public Vector3[] Control;
        public int GridSlots = 8;
        public float LateralG = 0.8f;
        public float MaxSpeedMs = 75f;
        /// <summary>Terrain undulation next to the track (m) and hill height toward the horizon (m).</summary>
        public float TerrainNear = 3f;
        public float TerrainFar = 60f;
        /// <summary>Distance over which the level roadside blends into the undulating terrain.</summary>
        public float TerrainBlend = 120f;
        /// <summary>Distance at which the hills reach full height.</summary>
        public float TerrainFarDistance = 700f;
        public Color CliffColor = new Color(0.42f, 0.4f, 0.38f);
        /// <summary>Night scenes light the world with explicit ambient colours instead of the dark sky.</summary>
        public bool NightAmbient;
        public Color AmbientSky = new Color(0.3f, 0.36f, 0.55f);
        public Color AmbientEquator = new Color(0.18f, 0.18f, 0.26f);
        public Color AmbientGround = new Color(0.06f, 0.06f, 0.08f);

        public Color Asphalt = new Color(0.24f, 0.24f, 0.25f);
        public Color Kerb = new Color(0.85f, 0.2f, 0.15f);
        /// <summary>Non-black makes the kerb stripes glow (night circuits).</summary>
        public Color KerbEmission = Color.black;
        public Color Barrier = new Color(0.82f, 0.82f, 0.85f);
        public Color Ground = new Color(0.36f, 0.42f, 0.24f);
        public Color SkyTint = new Color(0.45f, 0.6f, 0.85f);
        public Color SkyGround = new Color(0.32f, 0.3f, 0.28f);
        public float SkyExposure = 1.25f;
        public float Atmosphere = 0.95f;
        public Vector3 SunEuler = new Vector3(50f, -30f, 0f);
        public Color SunColor = new Color(1f, 0.96f, 0.9f);
        public float SunIntensity = 2.4f;
        public float AmbientIntensity = 1f;
        public bool Fog;
        public Color FogColor = Color.gray;
        public float FogDensity = 0.002f;

        public float LengthEstimate
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < Control.Length; i++)
                {
                    int next = (i + 1) % Control.Length;
                    if (!Loop && next == 0) break;
                    total += Vector3.Distance(Control[i], Control[next]);
                }
                return total;
            }
        }
    }

    public static class TrackSpecs
    {
        public static CircuitSpec[] All => new[]
        {
            SunsetLoop, MeridianDowntown, NeonLoop, DunePass, AlpineClimb, CargoYard, RidgeHighway, GrandCircuit
        };

        public static CircuitSpec SunsetLoop => new CircuitSpec
        {
            Id = ContentGenerator.CircuitTrackId, SceneName = ContentGenerator.CircuitSceneName, DisplayName = "Sunset Loop",
            Theme = TrackTheme.Coast, Dressing = "coast",
            Control = new[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 120f), new Vector3(0f, 0f, 240f), new Vector3(-25f, 0f, 320f),
                new Vector3(-95f, 0f, 350f), new Vector3(-160f, 0f, 320f), new Vector3(-190f, 0f, 250f), new Vector3(-175f, 0f, 170f),
                new Vector3(-215f, 0f, 110f), new Vector3(-200f, 0f, 30f), new Vector3(-235f, 0f, -50f), new Vector3(-205f, 0f, -110f),
                new Vector3(-140f, 0f, -95f), new Vector3(-90f, 0f, -40f), new Vector3(-60f, 0f, -110f), new Vector3(-10f, 0f, -130f),
                new Vector3(15f, 0f, -70f),
            },
            SunEuler = new Vector3(18f, 140f, 0f), SunColor = new Color(1f, 0.72f, 0.45f), SunIntensity = 2.6f, AmbientIntensity = 1.15f,
            SkyTint = new Color(0.6f, 0.45f, 0.5f), SkyExposure = 1.3f, Atmosphere = 1.1f,
        };

        public static CircuitSpec MeridianDowntown => new CircuitSpec
        {
            Id = "trk_city_circuit", SceneName = "Track_MeridianDowntown", DisplayName = "Meridian Downtown",
            Theme = TrackTheme.ModernCity, Dressing = "city", HalfWidth = 7f, TerrainNear = 0.5f, TerrainFar = 20f,
            Control = new[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 200f), new Vector3(0f, 0f, 400f), new Vector3(60f, 0f, 460f),
                new Vector3(200f, 0f, 460f), new Vector3(340f, 0f, 460f), new Vector3(400f, 0f, 400f), new Vector3(400f, 0f, 250f),
                new Vector3(340f, 0f, 190f), new Vector3(250f, 0f, 190f), new Vector3(200f, 0f, 120f), new Vector3(200f, 0f, 20f),
                new Vector3(140f, 0f, -40f), new Vector3(60f, 0f, -40f),
            },
            Asphalt = new Color(0.2f, 0.2f, 0.22f), Kerb = new Color(0.9f, 0.9f, 0.9f), Barrier = new Color(0.6f, 0.62f, 0.66f),
            Ground = new Color(0.3f, 0.3f, 0.32f), SkyTint = new Color(0.5f, 0.6f, 0.8f), SunEuler = new Vector3(58f, 20f, 0f),
        };

        public static CircuitSpec NeonLoop => new CircuitSpec
        {
            Id = "trk_night_run", SceneName = "Track_NeonLoop", DisplayName = "Neon Loop",
            Theme = TrackTheme.NightCity, Dressing = "night", HalfWidth = 7f,
            Control = new[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 150f), new Vector3(40f, 0f, 280f), new Vector3(150f, 0f, 340f),
                new Vector3(280f, 0f, 300f), new Vector3(330f, 0f, 190f), new Vector3(280f, 0f, 80f), new Vector3(330f, 0f, -30f),
                new Vector3(250f, 0f, -120f), new Vector3(120f, 0f, -100f), new Vector3(40f, 0f, -60f),
            },
            Asphalt = new Color(0.14f, 0.14f, 0.16f), Kerb = new Color(0.2f, 0.9f, 1f), Barrier = new Color(0.35f, 0.38f, 0.45f),
            Ground = new Color(0.12f, 0.12f, 0.15f), SkyTint = new Color(0.12f, 0.16f, 0.32f), SkyGround = new Color(0.08f, 0.08f, 0.12f),
            SkyExposure = 0.7f, Atmosphere = 0.5f, SunEuler = new Vector3(40f, -60f, 0f), SunColor = new Color(0.6f, 0.7f, 1f),
            SunIntensity = 1.4f, AmbientIntensity = 1.3f, Fog = true, FogColor = new Color(0.06f, 0.07f, 0.12f), FogDensity = 0.0018f,
            TerrainNear = 1f, TerrainFar = 25f, NightAmbient = true, KerbEmission = new Color(0.4f, 2.2f, 2.8f),
            AmbientSky = new Color(0.42f, 0.48f, 0.72f), AmbientEquator = new Color(0.26f, 0.27f, 0.38f), AmbientGround = new Color(0.1f, 0.1f, 0.14f),
        };

        public static CircuitSpec DunePass => new CircuitSpec
        {
            Id = "trk_dune_pass", SceneName = "Track_DunePass", DisplayName = "Dune Pass",
            Theme = TrackTheme.Desert, Dressing = "desert", HalfWidth = 7.5f, MaxSpeedMs = 85f, TerrainNear = 4f, TerrainFar = 90f,
            TerrainBlend = 200f, CliffColor = new Color(0.6f, 0.48f, 0.35f),
            Control = new[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 300f), new Vector3(-80f, 4f, 520f), new Vector3(-260f, 8f, 600f),
                new Vector3(-450f, 6f, 520f), new Vector3(-520f, 2f, 320f), new Vector3(-460f, 0f, 120f), new Vector3(-320f, 3f, 20f),
                new Vector3(-200f, 5f, -120f), new Vector3(-60f, 2f, -140f),
            },
            Asphalt = new Color(0.3f, 0.28f, 0.25f), Kerb = new Color(0.9f, 0.85f, 0.7f), Barrier = new Color(0.75f, 0.65f, 0.5f),
            Ground = new Color(0.78f, 0.66f, 0.42f), SkyTint = new Color(0.7f, 0.6f, 0.5f), SkyGround = new Color(0.6f, 0.5f, 0.35f),
            SkyExposure = 1.5f, Atmosphere = 0.8f, SunEuler = new Vector3(65f, 10f, 0f), SunColor = new Color(1f, 0.93f, 0.8f), SunIntensity = 2.8f,
        };

        public static CircuitSpec AlpineClimb => new CircuitSpec
        {
            Id = "trk_alpine_climb", SceneName = "Track_AlpineClimb", DisplayName = "Alpine Climb",
            Theme = TrackTheme.Mountains, Dressing = "mountain", Loop = false, HalfWidth = 6f, GridSlots = 8, TerrainNear = 6f, TerrainFar = 95f,
            TerrainBlend = 260f, TerrainFarDistance = 1400f, CliffColor = new Color(0.38f, 0.4f, 0.36f),
            // A climb that never doubles back over itself: stacked switchbacks turned the terrain
            // between them into a cliff wall, so every bend is at least 150 m from any other.
            Control = new[]
            {
                new Vector3(0f, 0f, -120f), new Vector3(0f, 0f, 0f), new Vector3(0f, 4f, 250f), new Vector3(-80f, 14f, 420f),
                new Vector3(-220f, 28f, 540f), new Vector3(-320f, 46f, 700f), new Vector3(-280f, 66f, 880f), new Vector3(-140f, 86f, 980f),
                new Vector3(20f, 104f, 1060f), new Vector3(180f, 122f, 1180f), new Vector3(260f, 142f, 1360f), new Vector3(200f, 160f, 1540f),
                new Vector3(60f, 176f, 1660f), new Vector3(-60f, 186f, 1820f),
            },
            Asphalt = new Color(0.26f, 0.26f, 0.27f), Kerb = new Color(0.85f, 0.85f, 0.85f), Barrier = new Color(0.55f, 0.55f, 0.58f),
            Ground = new Color(0.3f, 0.42f, 0.28f), SkyTint = new Color(0.55f, 0.7f, 0.9f), SkyExposure = 1.2f,
            SunEuler = new Vector3(40f, -50f, 0f), SunColor = new Color(1f, 0.98f, 0.95f), SunIntensity = 2.3f,
            Fog = true, FogColor = new Color(0.7f, 0.78f, 0.88f), FogDensity = 0.0012f,
        };

        public static CircuitSpec CargoYard => new CircuitSpec
        {
            Id = "trk_cargo_yard", SceneName = "Track_CargoYard", DisplayName = "Cargo Yard",
            Theme = TrackTheme.Industrial, Dressing = "industrial", HalfWidth = 6f, TerrainNear = 0.5f, TerrainFar = 15f,
            Control = new[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 120f), new Vector3(-40f, 0f, 180f), new Vector3(-120f, 0f, 180f),
                new Vector3(-160f, 0f, 120f), new Vector3(-120f, 0f, 60f), new Vector3(-160f, 0f, 0f), new Vector3(-240f, 0f, -20f),
                new Vector3(-260f, 0f, -120f), new Vector3(-180f, 0f, -180f), new Vector3(-80f, 0f, -160f), new Vector3(-40f, 0f, -80f),
            },
            Asphalt = new Color(0.22f, 0.22f, 0.22f), Kerb = new Color(0.95f, 0.75f, 0.1f), Barrier = new Color(0.35f, 0.36f, 0.4f),
            Ground = new Color(0.25f, 0.24f, 0.22f), SkyTint = new Color(0.5f, 0.55f, 0.6f), SkyExposure = 1f, Atmosphere = 1.2f,
            SunEuler = new Vector3(45f, 60f, 0f), SunIntensity = 2f, AmbientIntensity = 0.9f,
        };

        public static CircuitSpec RidgeHighway => new CircuitSpec
        {
            Id = "trk_ridge_highway", SceneName = "Track_RidgeHighway", DisplayName = "Ridge Highway",
            Theme = TrackTheme.Highway, Dressing = "highway", HalfWidth = 9f, MaxSpeedMs = 100f, LateralG = 0.85f,
            Control = new[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 600f), new Vector3(60f, 6f, 900f), new Vector3(260f, 12f, 1050f),
                new Vector3(560f, 10f, 1000f), new Vector3(700f, 4f, 800f), new Vector3(700f, 0f, 300f), new Vector3(600f, 0f, 60f),
                new Vector3(400f, 3f, -80f), new Vector3(150f, 2f, -60f),
            },
            Asphalt = new Color(0.25f, 0.25f, 0.26f), Kerb = new Color(0.95f, 0.95f, 0.95f), Barrier = new Color(0.7f, 0.72f, 0.75f),
            Ground = new Color(0.4f, 0.4f, 0.3f), SkyTint = new Color(0.5f, 0.62f, 0.85f), SunEuler = new Vector3(55f, -20f, 0f),
        };

        public static CircuitSpec GrandCircuit => new CircuitSpec
        {
            Id = "trk_grand_circuit", SceneName = "Track_GrandCircuit", DisplayName = "Grand Circuit",
            Theme = TrackTheme.RaceCircuit, Dressing = "circuit", HalfWidth = 8f, MaxSpeedMs = 95f, LateralG = 0.85f,
            Control = new[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 350f), new Vector3(-50f, 0f, 480f), new Vector3(-180f, 0f, 520f),
                new Vector3(-300f, 0f, 450f), new Vector3(-330f, 0f, 320f), new Vector3(-260f, 0f, 220f), new Vector3(-140f, 0f, 200f),
                new Vector3(-100f, 0f, 90f), new Vector3(-180f, 0f, -20f), new Vector3(-300f, 0f, -60f), new Vector3(-380f, 0f, -160f),
                new Vector3(-330f, 0f, -280f), new Vector3(-180f, 0f, -300f), new Vector3(-60f, 0f, -220f), new Vector3(40f, 0f, -120f),
            },
            Asphalt = new Color(0.22f, 0.22f, 0.23f), Kerb = new Color(0.9f, 0.15f, 0.12f), Barrier = new Color(0.9f, 0.9f, 0.92f),
            Ground = new Color(0.3f, 0.45f, 0.22f), SunEuler = new Vector3(52f, -35f, 0f),
        };
    }
}
