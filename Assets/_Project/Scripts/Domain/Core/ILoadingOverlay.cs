namespace RedlineLegends.Core
{
    /// <summary>Implemented by the persistent loading UI. Progress is 0..1.</summary>
    public interface ILoadingOverlay
    {
        void Show(string caption);
        void SetProgress(float progress01);
        void Hide();
        bool IsVisible { get; }
    }
}
