using UnityEngine;

namespace RedlineLegends.Core
{
    /// <summary>Shared VFX materials so every vehicle's particle systems batch on the same few materials.</summary>
    [CreateAssetMenu(fileName = "VfxLibrary", menuName = "Redline Legends/VFX Library")]
    public sealed class VfxLibrary : ScriptableObject
    {
        [SerializeField] private Material smoke;
        [SerializeField] private Material sparks;
        [SerializeField] private Material nitrous;
        [SerializeField] private Material skidMarks;
        [SerializeField] private int smokeMaxParticlesPlayer = 160;
        [SerializeField] private int smokeMaxParticlesAI = 48;
        [SerializeField] private int skidMarkSections = 1536;

        public Material Smoke => smoke;
        public Material Sparks => sparks;
        public Material Nitrous => nitrous;
        public Material SkidMarks => skidMarks;
        public int SmokeMaxParticlesPlayer => smokeMaxParticlesPlayer;
        public int SmokeMaxParticlesAI => smokeMaxParticlesAI;
        public int SkidMarkSections => skidMarkSections;

#if UNITY_EDITOR
        public void EditorInitialize(Material smokeMat, Material sparksMat, Material nitrousMat, Material skidMat)
        {
            smoke = smokeMat; sparks = sparksMat; nitrous = nitrousMat; skidMarks = skidMat;
        }
#endif
    }
}
