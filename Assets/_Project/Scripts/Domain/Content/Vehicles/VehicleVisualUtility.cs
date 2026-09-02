using System.Collections.Generic;
using UnityEngine;

namespace RedlineLegends.Vehicles
{
    /// <summary>
    /// Conventions a vehicle visual prefab must follow so gameplay and garage code stay generic:
    /// wheel transforms named Wheel_FL/FR/RL/RR, paintable renderers tagged by material name
    /// containing "Paint". Real car models only need to follow the naming to work.
    /// </summary>
    public static class VehicleVisualUtility
    {
        public const string WheelFL = "Wheel_FL";
        public const string WheelFR = "Wheel_FR";
        public const string WheelRL = "Wheel_RL";
        public const string WheelRR = "Wheel_RR";
        public const string PaintMaterialKeyword = "Paint";
        public const string CockpitCameraAnchor = "CockpitCamera";
        public const string ExhaustAnchor = "Exhaust";

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly List<Renderer> RendererBuffer = new List<Renderer>(32);
        private static MaterialPropertyBlock _block;

        public static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>Applies a paint option with a property block, so no material instances are created.</summary>
        public static void ApplyPaint(GameObject visual, VehicleDefinition definition, int paintIndex)
        {
            if (visual == null || definition == null) return;
            var paints = definition.PaintOptions;
            if (paints == null || paints.Length == 0) return;
            var paint = paints[Mathf.Clamp(paintIndex, 0, paints.Length - 1)];
            _block ??= new MaterialPropertyBlock();

            RendererBuffer.Clear();
            visual.GetComponentsInChildren(true, RendererBuffer);
            for (int i = 0; i < RendererBuffer.Count; i++)
            {
                var renderer = RendererBuffer[i];
                var materials = renderer.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                {
                    var mat = materials[m];
                    if (mat == null || !mat.name.Contains(PaintMaterialKeyword)) continue;
                    renderer.GetPropertyBlock(_block, m);
                    _block.SetColor(BaseColorId, paint.Color);
                    _block.SetFloat(MetallicId, paint.Metallic);
                    _block.SetFloat(SmoothnessId, paint.Smoothness);
                    renderer.SetPropertyBlock(_block, m);
                }
            }
        }
    }
}
