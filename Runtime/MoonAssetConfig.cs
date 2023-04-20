using UnityEngine;
using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Saro.MoonAsset
{
#if UNITY_EDITOR
    public static partial class MoonAssetConfig // Editor Part
    {
        public const string k_Editor_BuildGroupsPath = "Assets/MoonAsset/BuildGroups.asset";
        public const string k_Editor_SettingsPath = "Assets/MoonAsset/Settings.asset";
        public const string k_Editor_ManifestAssetPath = "Assets/MoonAsset/Manifest.asset";

        public readonly static string k_Editor_BuildOutputPath = $"ExtraAssets/Build/{GetCurrentPlatformName()}";
        public readonly static string k_Editor_DlcOutputPath = $"ExtraAssets/{k_Dlc}/{GetCurrentPlatformName()}";

        public readonly static string k_Editor_ResRawFolderPath = $"Assets/ResRaw";

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

    public partial class MoonAssetConfig // Runtime Part
    {
        public static string RemoteAssetUrl => MoonAsset.Current.RemoteAssetUrl;
        public static int s_MaxDownloadRetryCount = 3;

        /// <summary>
        /// 截取文件名前两个字符，作为文件夹
        /// <code>TODO 这类文件操作，做一个封装，另外 需要测试效率，判断什么时候开启比较合适</code>
        /// <code>考虑 开启后，编辑器也是这样弄好了，使用统一的接口，更方便</code>
        /// </summary>
        public static bool s_UseSubFolderForStorge = false;

        public const string k_Dlc = "DLC";
        public const string k_ManifestAsset = "manifest" + k_AssetExtension;
        public const string k_TmpManifestAsset = k_ManifestAsset + ".tmp";
        public const string k_AssetExtension = ".assets";
        public const string k_RawAssetExtension = ".raw";

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
        /// <code>新增对 <see cref="s_UseSubFolderForStorge"/> 的支持</code>
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetLocalAssetPath(string fileName)
        {
            if (fileName.StartsWith("/"))
                fileName = fileName.Substring(1);

            //if (s_UseSubFolderForStorge)
            //    fileName = GetCompatibleFileName(fileName);

            var fullPath = $"{k_DlcPath}/{fileName}";

            //if (s_UseSubFolderForStorge)
            //{
            //    var dir = Path.GetDirectoryName(fullPath);
            //    if (!Directory.Exists(dir))
            //    {
            //        Directory.CreateDirectory(dir);
            //    }
            //}

            return fullPath;
        }

        public static string GetCompatibleFileName(string fileName)
        {
            return fileName.Substring(0, 2) + "/" + fileName;
        }

        /// <summary>
        /// 获取当前平台的名字
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

        internal static bool PlatformUsesMultiThreading(RuntimePlatform platform)
        {
            return platform != RuntimePlatform.WebGLPlayer;
        }
    }
}
