using System;
using RedlineLegends.Events;
using RedlineLegends.Save;
using RedlineLegends.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>One line in an event list: either a championship header or a launchable event.</summary>
    public sealed class EventRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text starsText;
        [SerializeField] private Button launchButton;
        [SerializeField] private Image background;
        [SerializeField] private Color headerColor = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        [SerializeField] private Color eventColor = new Color(0.18f, 0.18f, 0.22f, 0.9f);
        [SerializeField] private Color lockedColor = new Color(0.14f, 0.14f, 0.16f, 0.6f);

        public void SetHeader(string title, string detail)
        {
            titleText.text = title;
            detailText.text = detail;
            starsText.text = "";
            launchButton.gameObject.SetActive(false);
            background.color = headerColor;
        }

        public void SetEvent(RaceEventDefinition evt, bool unlocked, EventProgressData progress, Action<RaceEventDefinition> onLaunch)
        {
            titleText.text = evt.DisplayName + (evt.IsBossEvent ? "  [BOSS]" : "");
            string track = evt.Track != null ? evt.Track.DisplayName : "?";
            string laps = evt is CircuitEventDefinition c && c.EventType != CircuitEventType.TimeAttack ? " · " + c.Laps + " laps" : "";
            string best = progress != null && progress.BestPosition > 0
                ? " · Best P" + progress.BestPosition + " " + MathUtil.FormatRaceTime(progress.BestTimeSeconds)
                : "";
            detailText.text = unlocked
                ? evt.ModeLabel + " · " + track + laps + " · PR " + evt.RecommendedPerformanceRating + best
                : "Locked · " + evt.UnlockRequirement.Describe();
            int stars = progress != null ? progress.Stars : 0;
            starsText.text = new string('★', stars) + new string('☆', 3 - stars);
            launchButton.gameObject.SetActive(true);
            launchButton.interactable = unlocked;
            launchButton.onClick.RemoveAllListeners();
            launchButton.onClick.AddListener(() => onLaunch(evt));
            background.color = unlocked ? eventColor : lockedColor;
        }

#if UNITY_EDITOR
        public void EditorWire(TMP_Text title, TMP_Text detail, TMP_Text stars, Button launch, Image bg)
        {
            titleText = title; detailText = detail; starsText = stars; launchButton = launch; background = bg;
        }
#endif
    }
}
