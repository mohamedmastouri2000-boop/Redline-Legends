using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RedlineLegends.Editor
{
    /// <summary>
    /// Creates the three URP quality tiers and the post-processing profile, and binds them to the
    /// project's quality levels. High targets a realistic look (HDR, ACES, SSAO, soft cascaded
    /// shadows); Low is the thermal-safe fallback for weak phones.
    /// </summary>
    public static class RenderPipelineSetup
    {
        public const string PackagePath = "Packages/com.unity.render-pipelines.universal";
        public const string HighAssetPath = EditorPaths.Settings + "/URP_High.asset";
        public const string MediumAssetPath = EditorPaths.Settings + "/URP_Medium.asset";
        public const string LowAssetPath = EditorPaths.Settings + "/URP_Low.asset";
        public const string VolumeProfilePath = EditorPaths.Settings + "/PostProcess_Global.asset";
        public static readonly string[] QualityNames = { "Low", "Medium", "High" };

        public static void Generate()
        {
            EditorPaths.EnsureFolder(EditorPaths.Settings);

            var low = CreateTier("Low", LowAssetPath, renderScale: 0.75f, msaa: 1, hdr: false,
                shadowDistance: 40f, cascades: 1, shadowRes: 1024, softShadows: false, ssao: false,
                additionalLights: LightRenderingMode.Disabled, depthTexture: false);
            var medium = CreateTier("Medium", MediumAssetPath, renderScale: 1f, msaa: 2, hdr: false,
                shadowDistance: 80f, cascades: 2, shadowRes: 2048, softShadows: true, ssao: false,
                additionalLights: LightRenderingMode.PerPixel, depthTexture: true);
            var high = CreateTier("High", HighAssetPath, renderScale: 1f, msaa: 4, hdr: true,
                shadowDistance: 150f, cascades: 4, shadowRes: 4096, softShadows: true, ssao: true,
                additionalLights: LightRenderingMode.PerPixel, depthTexture: true);

            CreateVolumeProfile();
            BindQualityLevels(low, medium, high);

            GraphicsSettings.defaultRenderPipeline = medium;
            EditorUtility.SetDirty(high);
            EditorUtility.SetDirty(medium);
            EditorUtility.SetDirty(low);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] URP tiers generated.");
        }

        private static UniversalRenderPipelineAsset CreateTier(string tier, string assetPath, float renderScale, int msaa, bool hdr,
            float shadowDistance, int cascades, int shadowRes, bool softShadows, bool ssao,
            LightRenderingMode additionalLights, bool depthTexture)
        {
            string rendererPath = assetPath.Replace(".asset", "_Renderer.asset");

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.name = "URP_" + tier + "_Renderer";
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }
            rendererData.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(PackagePath + "/Runtime/Data/PostProcessData.asset");
            ResourceReloader.ReloadAllNullIn(rendererData, PackagePath);
            rendererData.renderingMode = RenderingMode.Forward;
            // Mobile GPUs are tile based: a depth pre-pass costs more than it saves.
            rendererData.depthPrimingMode = DepthPrimingMode.Disabled;
            rendererData.copyDepthMode = CopyDepthMode.AfterOpaques;
            if (ssao) EnsureSsaoFeature(rendererData);
            EditorUtility.SetDirty(rendererData);

            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (asset == null)
            {
                asset = UniversalRenderPipelineAsset.Create(rendererData);
                asset.name = "URP_" + tier;
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            asset.renderScale = renderScale;
            asset.msaaSampleCount = msaa;
            asset.supportsHDR = hdr;
            asset.supportsCameraDepthTexture = depthTexture;
            asset.supportsCameraOpaqueTexture = false;
            asset.shadowDistance = shadowDistance;
            asset.shadowCascadeCount = cascades;
            asset.mainLightShadowmapResolution = shadowRes;
            asset.maxAdditionalLightsCount = additionalLights == LightRenderingMode.Disabled ? 0 : 4;
            asset.useSRPBatcher = true;
            // These have no public setters; write the serialized fields directly.
            var assetSo = new SerializedObject(asset);
            SetSerialized(assetSo, "m_MainLightShadowsSupported", p => p.boolValue = true);
            SetSerialized(assetSo, "m_SoftShadowsSupported", p => p.boolValue = softShadows);
            SetSerialized(assetSo, "m_AdditionalLightsRenderingMode", p => p.intValue = (int)additionalLights);
            SetSerialized(assetSo, "m_AdditionalLightShadowsSupported", p => p.boolValue = false);
            assetSo.ApplyModifiedPropertiesWithoutUndo();
            asset.colorGradingMode = hdr ? ColorGradingMode.HighDynamicRange : ColorGradingMode.LowDynamicRange;
            asset.colorGradingLutSize = 32;
            asset.shadowDepthBias = 1f;
            asset.shadowNormalBias = 1f;
            asset.cascade2Split = 0.25f;
            asset.upscalingFilter = UpscalingFilterSelection.Auto;
            return asset;
        }

        /// <summary>Adds the SSAO renderer feature through serialized properties (its settings type is internal).</summary>
        private static void EnsureSsaoFeature(UniversalRendererData rendererData)
        {
            var features = rendererData.rendererFeatures;
            for (int i = 0; i < features.Count; i++)
                if (features[i] is ScreenSpaceAmbientOcclusion) return;

            var feature = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            feature.name = "ScreenSpaceAmbientOcclusion";
            AssetDatabase.AddObjectToAsset(feature, rendererData);

            var featureSo = new SerializedObject(feature);
            var settings = featureSo.FindProperty("m_Settings");
            if (settings != null)
            {
                settings.FindPropertyRelative("Intensity").floatValue = 1.6f;
                settings.FindPropertyRelative("Radius").floatValue = 0.3f;
                settings.FindPropertyRelative("Downsample").boolValue = true;
                settings.FindPropertyRelative("AfterOpaque").boolValue = true;
                settings.FindPropertyRelative("DirectLightingStrength").floatValue = 0.25f;
                settings.FindPropertyRelative("Falloff").floatValue = 120f;
            }
            featureSo.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(rendererData);
            var list = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedPropertiesWithoutUndo();
            rendererData.SetDirty();
        }

        public static VolumeProfile CreateVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            var tonemapping = GetOrAdd<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);

            var bloom = GetOrAdd<Bloom>(profile);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.35f);
            bloom.scatter.Override(0.65f);
            bloom.highQualityFiltering.Override(false);

            var color = GetOrAdd<ColorAdjustments>(profile);
            color.postExposure.Override(0.15f);
            color.contrast.Override(8f);
            color.saturation.Override(6f);

            var vignette = GetOrAdd<Vignette>(profile);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.4f);

            var motionBlur = GetOrAdd<MotionBlur>(profile);
            motionBlur.intensity.Override(0.18f);
            motionBlur.quality.Override(MotionBlurQuality.Low);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var existing)) return existing;
            var component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        /// <summary>Rewrites the project quality levels to exactly Low/Medium/High bound to our URP assets.</summary>
        private static void BindQualityLevels(UniversalRenderPipelineAsset low, UniversalRenderPipelineAsset medium, UniversalRenderPipelineAsset high)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("[Setup] QualitySettings.asset not found.");
                return;
            }
            var so = new SerializedObject(assets[0]);
            var levels = so.FindProperty("m_QualitySettings");
            levels.arraySize = 3;
            var pipelines = new[] { low, medium, high };
            float[] lodBias = { 0.8f, 1.2f, 1.6f };
            int[] mipLimit = { 1, 0, 0 };
            int[] skinWeights = { 2, 2, 4 };
            for (int i = 0; i < 3; i++)
            {
                var level = levels.GetArrayElementAtIndex(i);
                Set(level, "name", p => p.stringValue = QualityNames[i]);
                Set(level, "customRenderPipeline", p => p.objectReferenceValue = pipelines[i]);
                Set(level, "vSyncCount", p => p.intValue = 0);
                Set(level, "antiAliasing", p => p.intValue = 0);
                Set(level, "lodBias", p => p.floatValue = lodBias[i]);
                Set(level, "globalTextureMipmapLimit", p => p.intValue = mipLimit[i]);
                Set(level, "textureQuality", p => p.intValue = mipLimit[i]);
                Set(level, "skinWeights", p => p.intValue = skinWeights[i]);
                Set(level, "particleRaycastBudget", p => p.intValue = 64 + i * 128);
                Set(level, "realtimeReflectionProbes", p => p.boolValue = i > 0);
                Set(level, "shadowDistance", p => p.floatValue = pipelines[i].shadowDistance);
                Set(level, "shadows", p => p.intValue = 2);
                Set(level, "shadowResolution", p => p.intValue = i + 1);
            }
            so.FindProperty("m_CurrentQuality").intValue = 1;

            var perPlatform = so.FindProperty("m_PerPlatformDefaultQuality");
            if (perPlatform != null)
            {
                for (int i = 0; i < perPlatform.arraySize; i++)
                {
                    var pair = perPlatform.GetArrayElementAtIndex(i);
                    var second = pair.FindPropertyRelative("second");
                    if (second != null) second.intValue = 1;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(SerializedProperty parent, string name, System.Action<SerializedProperty> apply)
        {
            var property = parent.FindPropertyRelative(name);
            if (property != null) apply(property);
        }

        private static void SetSerialized(SerializedObject so, string name, System.Action<SerializedProperty> apply)
        {
            var property = so.FindProperty(name);
            if (property != null) apply(property);
            else Debug.LogWarning("[Setup] Serialized field '" + name + "' not found on " + so.targetObject.name + "; URP version mismatch?");
        }
    }
}
