using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Saro.XAsset
{
#if UNITY_EDITOR
    public partial class XAssetConfig // Editor Part
    {
        public const string k_Editor_BuildGroupsPath = "Assets/XAsset/BuildGroups.asset";
        public const string k_Editor_SettingsPath = "Assets/XAsset/Settings.asset";
        public const string k_Editor_ManifestAssetPath = "Assets/XAsset/Manifest.asset";

        public readonly static string k_Editor_BuildOutputPath = $"ExtraAssets/Build/{GetCurrentPlatformName()}";
        public readonly static string k_Editor_DlcOutputPath = $"ExtraAssets/{k_Dlc}/{GetCurrentPlatformName()}";

        private static string GetPlatformName_Editor(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.iOS:
                    return "iOS";
                case BuildTarget.WebGL:
                    return "WebGL";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
                case BuildTarget.StandaloneOSX:
                    return "OSX";
                default:
                    return null;
            }
        }
    }
#endif

    public partial class XAssetConfig // Runtime Part
    {
        public static string RemoteAssetUrl => XAssetManager.Current.RemoteAssetUrl;
        public static int s_MaxDownloadRetryCount = 3;

        public const string k_Dlc = "DLC";
        public const string k_ManifestAsset = "manifest" + k_AssetExtension;
        public const string k_TmpManifestAsset = k_ManifestAsset + ".tmp";
        public const string k_AssetExtension = ".assets";

        public const string k_AssetBundleFoler = "Bundle";
        public const string k_CustomFolder = "Custom";

        public readonly static string k_BasePath = $"{Application.streamingAssetsPath}/{k_Dlc}/{GetCurrentPlatformName()}";
        public readonly static string k_DlcPath = $"{Application.persistentDataPath}/{k_Dlc}/{GetCurrentPlatformName()}";
        public readonly static string k_DownloadCachePath = $"{Application.persistentDataPath}/{k_Dlc}/{GetCurrentPlatformName()}/DownloadCache";

        public readonly static string k_ManifestAssetPath = $"{k_DlcPath}/{k_ManifestAsset}";
        public readonly static string k_TmpManifestAssetPath = $"{k_DlcPath}/{k_TmpManifestAsset}";

        /// <summary>
        /// 在文件末尾、扩展名前，追加hash
        /// </summary>
        /// <param name="path"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static string AppendHashToFileName(string path, string hash)
        {
            var dotIndex = path.LastIndexOf('.');

            hash = "_" + hash;
            if (dotIndex < 0)
            {
                dotIndex = path.Length;
            }

            return path.Insert(dotIndex, hash);
        }

        /// <summary>
        /// 远端资源，可以被下载下来
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetRemoteAssetURL(string fileName)
        {
            return GetRemoteAssetURL(RemoteAssetUrl, fileName);
        }

        /// <summary>
        /// 远端资源，可以被下载下来
        /// </summary>
        /// <param name="url"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetRemoteAssetURL(string url, string fileName)
        {
            if (fileName.StartsWith("/"))
                fileName = fileName.Substring(1);

            if (url != null && url.Length > 0)
                return $"{url}/{UnityEngine.Application.productName}/{k_Dlc}/{GetCurrentPlatformName()}/{fileName}";

            return $"{UnityEngine.Application.productName}/{k_Dlc}/{GetCurrentPlatformName()}/{fileName}";
        }

        /// <summary>
        /// 持久化目录（dlc）的资源
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetLocalAssetURL(string fileName)
        {
            if (fileName.StartsWith("/"))
                fileName = fileName.Substring(1);

            return $"{k_DlcPath}/{fileName}";
        }

        /// <summary>
        /// 获取当前平台AB包文件夹名字
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentPlatformName()
        {
#if UNITY_EDITOR
            return GetPlatformName_Editor(EditorUserBuildSettings.activeBuildTarget);
#else
            return GetPlatformName(Application.platform);
#endif
        }

        public static string GetPlatformName(RuntimePlatform target)
        {
            switch (target)
            {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "IOS";
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "Windows";
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return "OSX";
                case RuntimePlatform.WebGLPlayer:
                    return "WebGL";
                default:
                    return null;
            }
        }
    }
}
