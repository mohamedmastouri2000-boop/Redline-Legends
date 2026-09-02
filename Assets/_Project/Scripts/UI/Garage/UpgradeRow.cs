using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RedlineLegends.UI
{
    /// <summary>One upgrade category line in the garage: name, stage pips and a buy button.</summary>
    public sealed class UpgradeRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text buyLabel;

        public void Set(string upgradeName, int stage, int maxStage, int nextPrice, bool canBuy, string lockText, Action onBuy)
        {
            nameText.text = upgradeName;
            stageText.text = new string('●', stage) + new string('○', Mathf.Max(0, maxStage - stage));
            bool maxed = stage >= maxStage || nextPrice < 0;
            buyButton.gameObject.SetActive(!maxed);
            buyButton.interactable = canBuy;
            buyLabel.text = lockText ?? nextPrice.ToString("N0") + " CR";
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => onBuy());
        }

#if UNITY_EDITOR
        public void EditorWire(TMP_Text name, TMP_Text stage, Button buy, TMP_Text label)
        {
            nameText = name; stageText = stage; buyButton = buy; buyLabel = label;
        }
#endif
    }
}
