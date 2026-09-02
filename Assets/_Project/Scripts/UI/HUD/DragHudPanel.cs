using RedlineLegends.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>Drag-specific HUD: light tree, reaction time, shift feedback, and the two-lane gap bar.</summary>
    public sealed class DragHudPanel : MonoBehaviour
    {
        [SerializeField] private Image[] amberLights;
        [SerializeField] private Image greenLight;
        [SerializeField] private Image redLight;
        [SerializeField] private TMP_Text reactionText;
        [SerializeField] private TMP_Text shiftText;
        [SerializeField] private TMP_Text opponentText;
        [SerializeField] private RectTransform gapTrack;
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private RectTransform opponentMarker;
        [SerializeField] private Color lightOff = new Color(0.2f, 0.2f, 0.22f, 0.8f);
        [SerializeField] private Color amberOn = new Color(1f, 0.75f, 0.1f, 1f);
        [SerializeField] private Color greenOn = new Color(0.2f, 1f, 0.3f, 1f);
        [SerializeField] private Color redOn = new Color(1f, 0.15f, 0.1f, 1f);

        private float _shiftTimer;

        private void Awake()
        {
            SetLights(0, false);
            if (reactionText != null) reactionText.text = "";
            if (shiftText != null) shiftText.text = "";
            if (opponentText != null) opponentText.text = "";
        }

        public void SetLights(int stage, bool red)
        {
            for (int i = 0; i < amberLights.Length; i++)
                amberLights[i].color = !red && stage >= i + 1 && stage <= amberLights.Length ? amberOn : lightOff;
            if (greenLight != null) greenLight.color = !red && stage > amberLights.Length ? greenOn : lightOff;
            if (redLight != null) redLight.color = red ? redOn : lightOff;
        }

        public void SetReaction(float seconds, bool falseStart)
        {
            if (reactionText == null) return;
            reactionText.text = falseStart ? "RED LIGHT  " + seconds.ToString("+0.000;-0.000") + " s" : "RT " + seconds.ToString("0.000") + " s";
        }

        public void ShowShift(ShiftQuality quality)
        {
            if (shiftText == null) return;
            switch (quality)
            {
                case ShiftQuality.Perfect: shiftText.text = "PERFECT SHIFT"; shiftText.color = new Color(0.3f, 1f, 0.4f); break;
                case ShiftQuality.Good: shiftText.text = "GOOD SHIFT"; shiftText.color = new Color(0.6f, 0.9f, 1f); break;
                case ShiftQuality.Early: shiftText.text = "EARLY"; shiftText.color = new Color(1f, 0.8f, 0.3f); break;
                default: shiftText.text = "LATE"; shiftText.color = new Color(1f, 0.4f, 0.3f); break;
            }
            _shiftTimer = 1.2f;
        }

        /// <summary>Progress of both cars as 0..1 of the strip; gap text is the opponent's lead in metres.</summary>
        public void SetProgress(float player01, float opponent01, float gapMeters, string opponentName)
        {
            if (gapTrack != null)
            {
                float width = gapTrack.rect.width;
                if (playerMarker != null) playerMarker.anchoredPosition = new Vector2(Mathf.Clamp01(player01) * width, playerMarker.anchoredPosition.y);
                if (opponentMarker != null) opponentMarker.anchoredPosition = new Vector2(Mathf.Clamp01(opponent01) * width, opponentMarker.anchoredPosition.y);
            }
            if (opponentText != null)
                opponentText.text = Mathf.Abs(gapMeters) < 0.5f ? opponentName + "  even"
                    : gapMeters > 0f ? opponentName + "  +" + gapMeters.ToString("0.0") + " m" : opponentName + "  " + gapMeters.ToString("0.0") + " m";
        }

        private void Update()
        {
            if (_shiftTimer <= 0f || shiftText == null) return;
            _shiftTimer -= Time.deltaTime;
            if (_shiftTimer <= 0f) shiftText.text = "";
        }

#if UNITY_EDITOR
        public void EditorWire(Image[] ambers, Image green, Image red, TMP_Text reaction, TMP_Text shift, TMP_Text opponent,
            RectTransform track, RectTransform player, RectTransform opp)
        {
            amberLights = ambers; greenLight = green; redLight = red; reactionText = reaction; shiftText = shift; opponentText = opponent;
            gapTrack = track; playerMarker = player; opponentMarker = opp;
        }
#endif
    }
}
