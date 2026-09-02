using System.Collections.Generic;
using RedlineLegends.Core;
using RedlineLegends.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    public sealed class AchievementsPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button backButton;
        [SerializeField] private RectTransform content;
        [SerializeField] private AchievementRow rowTemplate;

        private readonly List<AchievementRow> _rows = new List<AchievementRow>();

        public Button BackButton => backButton;

        private void Awake()
        {
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
        }

        public void Refresh()
        {
            if (!Services.IsReady) return;
            var service = Services.Get<AchievementService>();
            var defs = service.Definitions;
            int unlocked = 0;
            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                bool done = service.IsUnlocked(def.Id);
                if (done) unlocked++;
                GetRow(i).Set(def.DisplayName, def.Description, service.GetProgress(def), def.Target, done, def.RewardCredits);
            }
            for (int i = defs.Count; i < _rows.Count; i++) _rows[i].gameObject.SetActive(false);
            if (titleText != null) titleText.text = "ACHIEVEMENTS  " + unlocked + "/" + defs.Count;
        }

        private AchievementRow GetRow(int index)
        {
            while (_rows.Count <= index)
            {
                var row = Instantiate(rowTemplate, content);
                row.name = "Achievement" + _rows.Count;
                _rows.Add(row);
            }
            _rows[index].gameObject.SetActive(true);
            return _rows[index];
        }

#if UNITY_EDITOR
        public void EditorWire(TMP_Text title, Button back, RectTransform list, AchievementRow template)
        {
            titleText = title; backButton = back; content = list; rowTemplate = template;
        }
#endif
    }
}
