using System;
using RedlineLegends.Events;
using RedlineLegends.Progression;
using UnityEngine;

namespace RedlineLegends.Career
{
    /// <summary>
    /// An ordered group of events. Career progression is a list of these; nothing about ordering or
    /// unlocking is hard-coded in UI.
    /// </summary>
    [CreateAssetMenu(fileName = "chp_new", menuName = "Redline Legends/Championship Definition")]
    public sealed class ChampionshipDefinition : ScriptableObject
    {
        [SerializeField] private string id = "chp_new";
        [SerializeField] private string displayName = "New Championship";
        [SerializeField, TextArea] private string description = "";
        [SerializeField] private int tier = 1;
        [SerializeField] private RaceEventDefinition[] events = Array.Empty<RaceEventDefinition>();
        [SerializeField] private UnlockRequirement unlockRequirement;
        [Tooltip("Bonus paid once when every event has been completed.")]
        [SerializeField] private int completionCredits = 5000;
        [SerializeField] private int completionXp = 1000;
        [SerializeField] private Sprite banner;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public int Tier => tier;
        public RaceEventDefinition[] Events => events;
        public UnlockRequirement UnlockRequirement => unlockRequirement;
        public int CompletionCredits => completionCredits;
        public int CompletionXp => completionXp;
        public Sprite Banner => banner;
        public int MaxStars => events.Length * 3;

#if UNITY_EDITOR
        public void EditorInitialize(string newId, string newName, string newDescription, int newTier,
            RaceEventDefinition[] newEvents, UnlockRequirement unlock, int credits, int xp)
        {
            id = newId; displayName = newName; description = newDescription; tier = newTier;
            events = newEvents; unlockRequirement = unlock; completionCredits = credits; completionXp = xp;
        }
#endif
    }
}
