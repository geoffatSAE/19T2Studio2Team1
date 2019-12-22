using System.IO;
using UnityEngine;
using UnityEditor;

namespace TO5
{
    /// <summary>
    /// Provides utility shared amongst projects
    /// </summary>
    public class Utility
    {
        // Directory to store asset bundles too. Handles if in editor or standalone
        public static string assetBundleDirectory
        {
            get
            {
                string bundlePlatform;
#if UNITY_EDITOR
                bundlePlatform = GetAssetBundlePlatform(EditorUserBuildSettings.activeBuildTarget);
#else
                bundlePlatform = GetAssetBundlePlatform(Application.platform);
#endif

                return Path.Combine(Application.streamingAssetsPath, bundlePlatform);
            }
        }

        private static string GetAssetBundlePlatform(RuntimePlatform platform)
        {
            // Supported platforms. If adding anything here be
            // sure to add it to GetAssetBundlesPlatform(BuildTarget)
            switch (platform)
            {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.WindowsPlayer:
                    return "Windows";
            }

            return "Unknown";
        }

#if UNITY_EDITOR
        private static string GetAssetBundlePlatform(BuildTarget buildTarget)
        {
            // Supported platform. If adding anything here be
            // sure to add it to GetAssetBundlesPlatform(RuntimePlatform)
            switch (buildTarget)
            {
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
            }

            return "Unknown";
        }
#endif
    }
}
