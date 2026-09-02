using System.Collections.Generic;
using RedlineLegends.Economy;
using RedlineLegends.Race;
using RedlineLegends.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>Post-race classification and the player's reward. Rows are cloned from a template.</summary>
    public sealed class ResultsPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private RectTransform content;
        [SerializeField] private ResultRow rowTemplate;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button restartButton;

        private readonly List<ResultRow> _rows = new List<ResultRow>();

        public Button ContinueButton => continueButton;
        public Button RestartButton => restartButton;

        private void Awake()
        {
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
        }

        public void Show(RaceOutcome outcome, RewardResult reward, bool practice)
        {
            gameObject.SetActive(true);
            var player = outcome.FindLocalPlayer();
            if (practice) titleText.text = "TEST DRIVE OVER";
            else if (outcome.Aborted || player == null) titleText.text = "RACE ABANDONED";
            else if (!player.Finished) titleText.text = "DID NOT FINISH";
            else titleText.text = player.Position == 1 ? "VICTORY" : "FINISHED P" + player.Position;

            rewardText.text = practice || outcome.Aborted || player == null || !player.Finished
                ? ""
                : "+" + reward.Credits.ToString("N0") + " CR   +" + reward.Xp.ToString("N0") + " XP   " +
                  UiText.Stars(reward.Stars) +
                  (reward.NewPersonalBest ? "   NEW BEST" : "");

            float leaderTime = outcome.Results.Count > 0 ? outcome.Results[0].TotalTimeSeconds : 0f;
            for (int i = 0; i < outcome.Results.Count; i++)
            {
                var result = outcome.Results[i];
                var row = GetRow(i);
                string time = result.Finished
                    ? (i == 0 ? MathUtil.FormatRaceTime(result.TotalTimeSeconds) : "+" + (result.TotalTimeSeconds - leaderTime).ToString("0.000"))
                    : "—";
                row.Set(result.Position, result.DisplayName, time, result.BestLapSeconds > 0f ? MathUtil.FormatRaceTime(result.BestLapSeconds) : "", result.ControlSource == ControlSource.LocalPlayer);
            }
            for (int i = outcome.Results.Count; i < _rows.Count; i++) _rows[i].gameObject.SetActive(false);
        }

        private ResultRow GetRow(int index)
        {
            while (_rows.Count <= index)
            {
                var row = Instantiate(rowTemplate, content);
                row.name = "Result" + _rows.Count;
                _rows.Add(row);
            }
            _rows[index].gameObject.SetActive(true);
            return _rows[index];
        }

#if UNITY_EDITOR
        public void EditorWire(TMP_Text title, TMP_Text reward, RectTransform list, ResultRow template, Button cont, Button restart)
        {
            titleText = title; rewardText = reward; content = list; rowTemplate = template; continueButton = cont; restartButton = restart;
        }
#endif
    }
}
