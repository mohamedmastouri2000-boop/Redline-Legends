using RedlineLegends.Core;
using RedlineLegends.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RedlineLegends.Editor
{
    /// <summary>Persistent app root: bootstrap, the single EventSystem and the loading curtain.</summary>
    public static class AppRootPrefabBuilder
    {
        public const string PrefabPath = EditorPaths.Resources + "/" + GameBootstrap.AppRootResourcePath + ".prefab";

        public static GameObject Build()
        {
            EditorPaths.EnsureFolder(EditorPaths.Resources);

            var root = new GameObject("AppRoot");
            root.AddComponent<GameBootstrap>();

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(root.transform, false);

            var canvas = UiKit.CreateCanvas("LoadingCanvas", 1000, root.transform);
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            var overlay = canvas.gameObject.AddComponent<LoadingOverlay>();

            var curtain = UiKit.CreatePanel(canvas.transform, "Curtain", new Color(0.03f, 0.03f, 0.04f, 1f));
            UiKit.Stretch((RectTransform)curtain.transform);

            var title = UiKit.CreateText(canvas.transform, "Title", "REDLINE LEGENDS", 64f, UiKit.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Anchor((RectTransform)title.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1200f, 90f));

            var caption = UiKit.CreateText(canvas.transform, "Caption", "Loading", 30f, UiKit.TextDim, TextAlignmentOptions.Center);
            UiKit.Anchor((RectTransform)caption.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1200f, 50f));

            var bar = UiKit.CreateFillBar(canvas.transform, "Progress", new Color(0.18f, 0.18f, 0.22f, 1f), UiKit.Accent, out var fill);
            UiKit.Anchor((RectTransform)bar.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(720f, 10f));

            overlay.EditorWire(group, caption, fill);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[Setup] AppRoot prefab written to " + PrefabPath);
            return prefab;
        }
    }
}
