using System;
using RedlineLegends.Core;

namespace RedlineLegends.Progression
{
    public static class TutorialIds
    {
        public const string FirstCircuit = "tut_first_circuit";
        public const string FirstDrag = "tut_first_drag";
        public const string FirstUpgrade = "tut_first_upgrade";
    }

    /// <summary>One tutorial page: a title and a short body. Kept as data so text can move to localisation later.</summary>
    [Serializable]
    public struct TutorialPage
    {
        public string Title;
        public string Body;

        public TutorialPage(string title, string body)
        {
            Title = title;
            Body = body;
        }
    }

    /// <summary>Decides whether a tutorial should show (enabled in settings and not yet completed) and records completion.</summary>
    public sealed class TutorialService
    {
        private readonly ProgressionService _progression;
        private readonly SettingsService _settings;

        public TutorialService(ProgressionService progression, SettingsService settings)
        {
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool ShouldShow(string tutorialId)
            => _settings.Current.TutorialsEnabled && !_progression.IsTutorialCompleted(tutorialId);

        public void Complete(string tutorialId) => _progression.MarkTutorialCompleted(tutorialId);

        public static TutorialPage[] PagesFor(string tutorialId)
        {
            switch (tutorialId)
            {
                case TutorialIds.FirstCircuit:
                    return new[]
                    {
                        new TutorialPage("Steering", "Steer with the arrows, the wheel or by tilting the phone (change it in Settings). Hold GAS to accelerate."),
                        new TutorialPage("Corners", "Brake before a corner, not in it. Pass every checkpoint gate in order; missing one does not count."),
                        new TutorialPage("Nitrous", "Hold NOS on straights when the bar has charge. The RESET button returns you to the last gate.")
                    };
                case TutorialIds.FirstDrag:
                    return new[]
                    {
                        new TutorialPage("Staging", "Hold GAS on the brakes to build revs. Around 60% of the redline launches cleanly; more spins the tyres."),
                        new TutorialPage("The lights", "Leave on GREEN. Moving before green is a red light and you lose the run."),
                        new TutorialPage("Shifting", "In manual, shift when the shift light comes on for a PERFECT shift. Use NOS once the car is straight.")
                    };
                case TutorialIds.FirstUpgrade:
                    return new[]
                    {
                        new TutorialPage("Upgrades", "Upgrades change the real car: torque, grip, weight, brakes. Your Performance Rating (PR) shows the result."),
                        new TutorialPage("Entry limits", "Events list a recommended PR and class. TUNE adjusts the setup; TEST DRIVE lets you feel it before racing.")
                    };
                default:
                    return Array.Empty<TutorialPage>();
            }
        }
    }
}
