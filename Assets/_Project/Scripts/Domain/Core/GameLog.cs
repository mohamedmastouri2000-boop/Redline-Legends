using System.Diagnostics;

namespace RedlineLegends.Core
{
    /// <summary>
    /// Thin logging facade. Info logs are compiled out of release builds; warnings and errors stay.
    /// </summary>
    public static class GameLog
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string message) => UnityEngine.Debug.Log(message);

        public static void Warn(string message) => UnityEngine.Debug.LogWarning(message);

        public static void Error(string message) => UnityEngine.Debug.LogError(message);

        public static void Exception(System.Exception exception, UnityEngine.Object context = null)
            => UnityEngine.Debug.LogException(exception, context);
    }
}
