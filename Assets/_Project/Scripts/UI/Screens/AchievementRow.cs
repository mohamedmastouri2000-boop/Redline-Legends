using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    public sealed class AchievementRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Image background;
        [SerializeField] private Color lockedColor = new Color(0.14f, 0.14f, 0.18f, 0.9f);
        [SerializeField] private Color unlockedColor = new Color(0.16f, 0.32f, 0.18f, 0.95f);

        public void Set(string name, string description, int progress, int target, bool unlocked, int rewardCredits)
        {
            nameText.text = name;
            descriptionText.text = description + "   (+" + rewardCredits.ToString("N0") + " CR)";
            progressText.text = unlocked ? "DONE" : progress + "/" + target;
            background.color = unlocked ? unlockedColor : lockedColor;
        }

#if UNITY_EDITOR
        public void EditorWire(TMP_Text name, TMP_Text desc, TMP_Text progress, Image bg)
        {
            nameText = name; descriptionText = desc; progressText = progress; background = bg;
        }
#endif
    }
}
