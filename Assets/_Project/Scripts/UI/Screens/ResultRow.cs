using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>One classification line in the results panel.</summary>
    public sealed class ResultRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text positionText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text lapText;
        [SerializeField] private Image background;
        [SerializeField] private Color normalColor = new Color(0.14f, 0.14f, 0.18f, 0.9f);
        [SerializeField] private Color playerColor = new Color(0.45f, 0.1f, 0.1f, 0.95f);

        public void Set(int position, string name, string time, string bestLap, bool isPlayer)
        {
            positionText.text = position.ToString();
            nameText.text = name;
            timeText.text = time;
            lapText.text = bestLap;
            background.color = isPlayer ? playerColor : normalColor;
        }

#if UNITY_EDITOR
        public void EditorWire(TMP_Text pos, TMP_Text name, TMP_Text time, TMP_Text lap, Image bg)
        {
            positionText = pos; nameText = name; timeText = time; lapText = lap; background = bg;
        }
#endif
    }
}
