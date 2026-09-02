using System;
using System.Collections.Generic;
using RedlineLegends.Content;
using RedlineLegends.Core;
using RedlineLegends.Events;
using RedlineLegends.Progression;
using RedlineLegends.Race;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>
    /// Lists championships and their events for one race mode, showing unlock state and the
    /// player's best result. Rows are cloned from a template child so no prefab is needed.
    /// </summary>
    public sealed class EventListPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button backButton;
        [SerializeField] private RectTransform content;
        [SerializeField] private EventRow rowTemplate;

        private readonly List<EventRow> _rows = new List<EventRow>();
        private RaceMode _mode;
        private Action<RaceEventDefinition> _onLaunch;

        public void Initialize(RaceMode mode, Action<RaceEventDefinition> onLaunch, Action onBack)
        {
            _mode = mode;
            _onLaunch = onLaunch;
            titleText.text = mode == RaceMode.Drag ? "DRAG RACING" : "CIRCUIT RACING";
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => onBack());
            rowTemplate.gameObject.SetActive(false);
        }

        public void Refresh()
        {
            var catalog = Services.Get<ContentCatalog>();
            var progression = Services.Get<ProgressionService>();

            int used = 0;
            foreach (var championship in catalog.Championships)
            {
                bool any = false;
                foreach (var evt in championship.Events)
                    if (evt != null && evt.Mode == _mode) { any = true; break; }
                if (!any) continue;

                bool championshipUnlocked = progression.IsChampionshipUnlocked(championship);
                var header = GetRow(used++);
                header.SetHeader(championship.DisplayName, championshipUnlocked
                    ? progression.GetChampionshipStars(championship.Id) + "/" + championship.MaxStars + " stars"
                    : championship.UnlockRequirement.Describe());

                foreach (var evt in championship.Events)
                {
                    if (evt == null || evt.Mode != _mode) continue;
                    var row = GetRow(used++);
                    bool unlocked = championshipUnlocked && progression.IsEventUnlocked(evt);
                    var progress = progression.FindEvent(evt.Id);
                    row.SetEvent(evt, unlocked, progress, _onLaunch);
                }
            }
            for (int i = used; i < _rows.Count; i++) _rows[i].gameObject.SetActive(false);
        }

        private EventRow GetRow(int index)
        {
            while (_rows.Count <= index)
            {
                var row = Instantiate(rowTemplate, content);
                row.name = "Row" + _rows.Count;
                _rows.Add(row);
            }
            _rows[index].gameObject.SetActive(true);
            return _rows[index];
        }

#if UNITY_EDITOR
        public void EditorWire(TMP_Text title, Button back, RectTransform contentRoot, EventRow template)
        {
            titleText = title; backButton = back; content = contentRoot; rowTemplate = template;
        }
#endif
    }
}
