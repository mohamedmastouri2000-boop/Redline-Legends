using RedlineLegends.Core;
using RedlineLegends.Utilities;
using RedlineLegends.Vehicles;
using UnityEngine;

namespace RedlineLegends.Audio
{
    /// <summary>
    /// Engine, tyre, wind, nitrous and impact audio for one car, driven from telemetry. Uses the
    /// vehicle's VehicleAudioDefinition layers when they exist and procedural placeholders when
    /// they do not. All sources are created once; nothing is instantiated during a race.
    /// </summary>
    public sealed class VehicleAudio : MonoBehaviour
    {
        private VehicleController _car;
        private VehicleAudioDefinition _definition;
        private AudioService _audio;
        private bool _isPlayer;

        private AudioSource[] _engineLayers = new AudioSource[0];
        private float[] _layerNativeRpm = new float[0];
        private AudioSource _tyre;
        private AudioSource _wind;
        private AudioSource _nitrous;
        private AudioSource _oneShot;
        private float _engineVolume = 0.8f;
        private float _minPitch = 0.6f;
        private float _maxPitch = 1.8f;
        private float _lastLimiterTime;

        public void Initialize(VehicleController car, VehicleAudioDefinition definition, bool isPlayer)
        {
            _car = car;
            _definition = definition;
            _isPlayer = isPlayer;
            Services.TryGet(out _audio);

            float spatial = isPlayer ? 0.15f : 1f;
            if (definition != null)
            {
                _engineVolume = definition.EngineVolume;
                _minPitch = definition.MinPitch;
                _maxPitch = definition.MaxPitch;
            }

            var layers = definition != null ? definition.EngineOnLayers : null;
            if (layers != null && layers.Length > 0)
            {
                _engineLayers = new AudioSource[layers.Length];
                _layerNativeRpm = new float[layers.Length];
                for (int i = 0; i < layers.Length; i++)
                {
                    _engineLayers[i] = CreateSource("Engine" + i, layers[i].Loop, true, spatial);
                    _layerNativeRpm[i] = Mathf.Max(500f, layers[i].NativeRpm);
                }
            }
            else
            {
                _engineLayers = new[] { CreateSource("Engine", ProceduralAudioClips.Engine, true, spatial) };
                _layerNativeRpm = new[] { 0f }; // 0 = pitch from normalized rpm range
            }
            _tyre = CreateSource("Tyres", definition != null && definition.TireSquealLoop != null ? definition.TireSquealLoop : ProceduralAudioClips.Noise, true, spatial);
            _wind = CreateSource("Wind", definition != null && definition.WindLoop != null ? definition.WindLoop : ProceduralAudioClips.Noise, true, spatial);
            _wind.pitch = 0.5f;
            _nitrous = CreateSource("Nitrous", definition != null && definition.NitrousLoop != null ? definition.NitrousLoop : ProceduralAudioClips.Hiss, true, spatial);
            _nitrous.pitch = 1.6f;
            _oneShot = CreateSource("OneShot", null, false, spatial);

            for (int i = 0; i < _engineLayers.Length; i++) _engineLayers[i].Play();
            _tyre.Play();
            _wind.Play();
            _nitrous.Play();
            _tyre.volume = _wind.volume = _nitrous.volume = 0f;

            _car.Shifted += OnShifted;
            _car.Collided += OnCollided;
            _car.LimiterHit += OnLimiter;
        }

        private AudioSource CreateSource(string name, AudioClip clip, bool loop, float spatialBlend)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 6f;
            source.maxDistance = 90f;
            source.priority = _isPlayer ? 32 : 160;
            return source;
        }

        private void OnDestroy()
        {
            if (_car == null) return;
            _car.Shifted -= OnShifted;
            _car.Collided -= OnCollided;
            _car.LimiterHit -= OnLimiter;
        }

        private void Update()
        {
            if (_car == null || !_car.IsInitialized) return;
            var tel = _car.Telemetry;
            float sfx = _audio != null ? _audio.Sfx : 1f;

            // Engine: throttle lifts the volume, rpm sets pitch; multiple layers crossfade by native rpm.
            float load = Mathf.Lerp(0.55f, 1f, tel.Throttle);
            float rpm = Mathf.Max(tel.IdleRpm, tel.Rpm);
            if (_engineLayers.Length == 1 && _layerNativeRpm[0] <= 0f)
            {
                _engineLayers[0].pitch = Mathf.Lerp(_minPitch, _maxPitch, tel.RpmNormalized);
                _engineLayers[0].volume = _engineVolume * load * sfx;
            }
            else
            {
                for (int i = 0; i < _engineLayers.Length; i++)
                {
                    float native = _layerNativeRpm[i];
                    float prev = i > 0 ? _layerNativeRpm[i - 1] : native * 0.5f;
                    float next = i < _engineLayers.Length - 1 ? _layerNativeRpm[i + 1] : native * 1.6f;
                    float weight = rpm < native ? Mathf.InverseLerp(prev, native, rpm) : 1f - Mathf.InverseLerp(native, next, rpm);
                    _engineLayers[i].pitch = rpm / native;
                    _engineLayers[i].volume = _engineVolume * load * sfx * Mathf.Clamp01(weight);
                }
            }

            _tyre.volume = Mathf.Clamp01((tel.MaxSlip - 0.25f) * 1.6f) * (tel.IsAirborne ? 0f : 0.7f) * sfx;
            _tyre.pitch = 0.9f + tel.MaxSlip * 0.3f;
            float speed01 = Mathf.Clamp01(tel.SpeedKmh / 220f);
            _wind.volume = speed01 * speed01 * (_isPlayer ? 0.45f : 0.15f) * sfx;
            _nitrous.volume = tel.NitrousActive ? 0.5f * sfx : 0f;
        }

        private void OnShifted(int from, int to, float rpm, ShiftQuality quality)
        {
            var clip = _definition != null && _definition.GearShift != null ? _definition.GearShift : ProceduralAudioClips.Click;
            _oneShot.pitch = quality == ShiftQuality.Perfect ? 1.3f : 1f;
            _oneShot.PlayOneShot(clip, 0.7f * (_audio != null ? _audio.Sfx : 1f));
        }

        private void OnLimiter()
        {
            if (Time.time - _lastLimiterTime < 0.12f) return;
            _lastLimiterTime = Time.time;
            var clip = _definition != null && _definition.LimiterBounce != null ? _definition.LimiterBounce : ProceduralAudioClips.Click;
            _oneShot.pitch = 0.7f;
            _oneShot.PlayOneShot(clip, 0.35f * (_audio != null ? _audio.Sfx : 1f));
        }

        private void OnCollided(float impulse, Vector3 point)
        {
            AudioClip clip = null;
            if (_definition != null)
            {
                var set = impulse > 6f ? _definition.CollisionHeavy : _definition.CollisionLight;
                if (set != null && set.Length > 0) clip = set[Random.Range(0, set.Length)];
            }
            if (clip == null) clip = ProceduralAudioClips.Impact;
            _oneShot.pitch = Random.Range(0.85f, 1.15f);
            _oneShot.PlayOneShot(clip, Mathf.Clamp01(impulse / 8f) * (_audio != null ? _audio.Sfx : 1f));
        }
    }
}
