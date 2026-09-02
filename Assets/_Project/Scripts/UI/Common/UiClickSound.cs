using RedlineLegends.Audio;
using RedlineLegends.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>Plays a click for every Button under this canvas. One AudioSource per canvas, no per-button setup.</summary>
    public sealed class UiClickSound : MonoBehaviour
    {
        [SerializeField] private AudioClip clip;
        private AudioSource _source;
        private AudioService _audio;

        private void Start()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            Services.TryGet(out _audio);
            var buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].onClick.AddListener(Play);
        }

        private void Play()
        {
            var c = clip != null ? clip : ProceduralAudioClips.Click;
            _source.pitch = 1.1f;
            _source.PlayOneShot(c, 0.5f * (_audio != null ? _audio.Sfx : 1f));
        }
    }
}
