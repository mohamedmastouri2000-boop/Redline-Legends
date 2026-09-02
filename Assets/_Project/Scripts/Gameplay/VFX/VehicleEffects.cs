using RedlineLegends.Core;
using RedlineLegends.Vehicles;
using UnityEngine;

namespace RedlineLegends.VFX
{
    /// <summary>
    /// Tyre smoke, skid marks, exhaust backfire, nitrous flame and collision sparks for one car.
    /// Every particle system is created once at spawn; effects only change emission rates or call
    /// Emit, so a race never instantiates or destroys effect objects.
    /// </summary>
    public sealed class VehicleEffects : MonoBehaviour
    {
        private VehicleController _car;
        private VfxLibrary _vfx;
        private SkidMarkRenderer _skids;
        private bool _isPlayer;
        private ParticleSystem[] _smoke = new ParticleSystem[0];
        private ParticleSystem _sparks;
        private ParticleSystem _nitrous;
        private ParticleSystem _backfire;
        private int[] _skidIndex = new int[0];
        private float _lastBackfire;
        private Transform _exhaust;

        public void Initialize(VehicleController car, VfxLibrary vfx, SkidMarkRenderer skids, bool isPlayer)
        {
            _car = car;
            _vfx = vfx;
            _skids = skids;
            _isPlayer = isPlayer;
            if (_vfx == null) return;

            var wheels = car.Wheels;
            _smoke = new ParticleSystem[wheels.Length];
            _skidIndex = new int[wheels.Length];
            int maxSmoke = isPlayer ? vfx.SmokeMaxParticlesPlayer : vfx.SmokeMaxParticlesAI;
            for (int i = 0; i < wheels.Length; i++)
            {
                _smoke[i] = CreateSystem("Smoke_" + wheels[i].Name, vfx.Smoke, maxSmoke, new Color(0.85f, 0.85f, 0.85f, 0.35f),
                    startSize: 0.8f, sizeGrowth: 2.4f, lifetime: 1.4f, speed: 0.6f, gravity: -0.05f, worldSpace: true);
                _skidIndex[i] = -1;
            }
            _exhaust = VehicleVisualUtility.FindDeep(car.transform, VehicleVisualUtility.ExhaustAnchor) ?? car.transform;
            _sparks = CreateSystem("Sparks", vfx.Sparks, 120, new Color(1f, 0.75f, 0.35f, 1f), 0.12f, 0.2f, 0.5f, 6f, 2.5f, true);
            _nitrous = CreateSystem("Nitrous", vfx.Nitrous, 60, new Color(0.35f, 0.6f, 1f, 0.9f), 0.35f, 0.1f, 0.25f, 9f, 0f, false);
            _nitrous.transform.SetParent(_exhaust, false);
            _nitrous.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            _backfire = CreateSystem("Backfire", vfx.Sparks, 40, new Color(1f, 0.55f, 0.15f, 1f), 0.4f, 0.6f, 0.15f, 5f, 0f, false);
            _backfire.transform.SetParent(_exhaust, false);
            _backfire.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            _car.Collided += OnCollided;
            _car.LimiterHit += OnLimiter;
            _car.Shifted += OnShifted;
        }

        private ParticleSystem CreateSystem(string name, Material material, int maxParticles, Color color, float startSize,
            float sizeGrowth, float lifetime, float speed, float gravity, bool worldSpace)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.maxParticles = maxParticles;
            main.startColor = color;
            main.startSize = startSize;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.gravityModifier = gravity;
            main.simulationSpace = worldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.loop = true;
            main.playOnAwake = false;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.15f;
            var size = ps.sizeOverLifetime;
            size.enabled = sizeGrowth > 0f;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 1f + sizeGrowth));
            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = gradient;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingFudge = 0f;
            ps.Play();
            return ps;
        }

        private void OnDestroy()
        {
            if (_car == null) return;
            _car.Collided -= OnCollided;
            _car.LimiterHit -= OnLimiter;
            _car.Shifted -= OnShifted;
        }

        private void Update()
        {
            if (_car == null || !_car.IsInitialized || _vfx == null) return;
            var wheels = _car.Wheels;
            var tel = _car.Telemetry;
            for (int i = 0; i < wheels.Length && i < _smoke.Length; i++)
            {
                var w = wheels[i];
                float slip = w.Grounded ? w.SlipAmount : 0f;
                var emission = _smoke[i].emission;
                emission.rateOverTime = slip > 0.3f ? Mathf.Lerp(0f, _isPlayer ? 45f : 18f, (slip - 0.3f) / 0.7f) : 0f;
                if (w.Grounded) _smoke[i].transform.position = w.ContactPoint + w.ContactNormal * 0.1f;

                if (_skids != null)
                {
                    if (w.Grounded && slip > 0.35f)
                        _skidIndex[i] = _skids.AddSection(w.ContactPoint, w.ContactNormal, (slip - 0.35f) / 0.65f, _skidIndex[i]);
                    else _skidIndex[i] = -1;
                }
            }
            var nos = _nitrous.emission;
            nos.rateOverTime = tel.NitrousActive ? 80f : 0f;
        }

        private void OnCollided(float impulse, Vector3 point)
        {
            if (impulse < 1.5f) return;
            _sparks.transform.position = point;
            _sparks.Emit(Mathf.Clamp(Mathf.RoundToInt(impulse * 4f), 6, 40));
        }

        private void OnLimiter()
        {
            if (Time.time - _lastBackfire < 0.25f) return;
            _lastBackfire = Time.time;
            _backfire.Emit(3);
        }

        private void OnShifted(int from, int to, float rpm, ShiftQuality quality)
        {
            if (to > from && (quality == ShiftQuality.Late || quality == ShiftQuality.Perfect)) _backfire.Emit(quality == ShiftQuality.Late ? 8 : 3);
        }
    }
}
