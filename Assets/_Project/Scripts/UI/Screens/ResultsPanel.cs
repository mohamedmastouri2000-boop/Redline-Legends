using System.Collections;
using System.Collections.Generic;
using RedlineLegends.Economy;
using RedlineLegends.Race;
using RedlineLegends.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>
    /// Post-race classification and the player's reward. Rows are cloned from a template; the
    /// credit and XP figures count up over a second so the payout registers.
    /// </summary>
    public sealed class ResultsPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private RectTransform content;
        [SerializeField] private ResultRow rowTemplate;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private float countUpSeconds = 1.1f;

        private readonly List<ResultRow> _rows = new List<ResultRow>();
        private Coroutine _countUp;

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

            bool rewarded = !(practice || outcome.Aborted || player == null || !player.Finished);
            if (_countUp != null) StopCoroutine(_countUp);
            if (rewarded)
            {
                rewardText.text = RewardLine(0, 0, reward);
                _countUp = isActiveAndEnabled ? StartCoroutine(CountUp(reward)) : null;
                if (_countUp == null) rewardText.text = RewardLine(reward.Credits, reward.Xp, reward);
            }
            else rewardText.text = "";

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

        private static string RewardLine(long credits, long xp, RewardResult reward)
        {
            return "+" + credits.ToString("N0") + " CR   +" + xp.ToString("N0") + " XP   " + UiText.Stars(reward.Stars) +
                   (reward.NewPersonalBest ? "   NEW BEST" : "");
        }

        private IEnumerator CountUp(RewardResult reward)
        {
            float t = 0f;
            while (t < countUpSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / countUpSeconds), 3f);
                rewardText.text = RewardLine((long)Mathf.Round(reward.Credits * k), (long)Mathf.Round(reward.Xp * k), reward);
                yield return null;
            }
            rewardText.text = RewardLine(reward.Credits, reward.Xp, reward);
            _countUp = null;
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
