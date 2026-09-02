using System;
using UnityEngine;

namespace RedlineLegends.Audio
{
    /// <summary>One engine loop and the rpm it was recorded at; the engine mixer crossfades between layers.</summary>
    [Serializable]
    public sealed class EngineAudioLayer
    {
        public AudioClip Loop;
        [Tooltip("RPM at which this loop plays at pitch 1.0.")]
        public float NativeRpm = 3000f;
    }

    /// <summary>
    /// Every sound a car makes. Referenced by VehicleDefinition; several cars can share one asset.
    /// Missing clips are allowed (the audio component simply skips that layer) so placeholder
    /// vehicles work before final audio lands.
    /// </summary>
    [CreateAssetMenu(fileName = "aud_vehicle", menuName = "Redline Legends/Vehicle Audio Definition")]
    public sealed class VehicleAudioDefinition : ScriptableObject
    {
        [SerializeField] private string id = "aud_vehicle";
        [Header("Engine")]
        [SerializeField] private EngineAudioLayer[] engineOnLayers = Array.Empty<EngineAudioLayer>();
        [SerializeField] private EngineAudioLayer[] engineOffLayers = Array.Empty<EngineAudioLayer>();
        [SerializeField] private AudioClip idleLoop;
        [SerializeField] private AudioClip gearShift;
        [SerializeField] private AudioClip limiterBounce;
        [Header("Forced induction")]
        [SerializeField] private AudioClip turboLoop;
        [SerializeField] private AudioClip turboBlowOff;
        [SerializeField] private AudioClip[] backfires = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip nitrousLoop;
        [Header("Chassis")]
        [SerializeField] private AudioClip tireSquealLoop;
        [SerializeField] private AudioClip[] collisionLight = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] collisionHeavy = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip windLoop;
        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float engineVolume = 0.8f;
        [SerializeField] private float minPitch = 0.6f;
        [SerializeField] private float maxPitch = 1.8f;

        public string Id => id;
        public EngineAudioLayer[] EngineOnLayers => engineOnLayers;
        public EngineAudioLayer[] EngineOffLayers => engineOffLayers;
        public AudioClip IdleLoop => idleLoop;
        public AudioClip GearShift => gearShift;
        public AudioClip LimiterBounce => limiterBounce;
        public AudioClip TurboLoop => turboLoop;
        public AudioClip TurboBlowOff => turboBlowOff;
        public AudioClip[] Backfires => backfires;
        public AudioClip NitrousLoop => nitrousLoop;
        public AudioClip TireSquealLoop => tireSquealLoop;
        public AudioClip[] CollisionLight => collisionLight;
        public AudioClip[] CollisionHeavy => collisionHeavy;
        public AudioClip WindLoop => windLoop;
        public float EngineVolume => engineVolume;
        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;

#if UNITY_EDITOR
        public void EditorInitialize(string newId) { id = newId; }
#endif
    }
}
