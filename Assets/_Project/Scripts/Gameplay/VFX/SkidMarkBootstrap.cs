using RedlineLegends.Core;
using UnityEngine;

namespace RedlineLegends.VFX
{
    /// <summary>Initialises the scene's SkidMarkRenderer from the VFX library on load (before any car spawns).</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class SkidMarkBootstrap : MonoBehaviour
    {
        [SerializeField] private SkidMarkRenderer target;
        [SerializeField] private VfxLibrary library;

        private void Awake()
        {
            if (target != null && library != null) target.Initialize(library.SkidMarkSections, library.SkidMarks);
        }

#if UNITY_EDITOR
        public void EditorWire(SkidMarkRenderer renderer, VfxLibrary vfx)
        {
            target = renderer;
            library = vfx;
        }
#endif
    }
}
