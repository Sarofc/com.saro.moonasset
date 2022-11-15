using Saro.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Saro.MoonAsset.Build
{
    internal partial class BuildMethods : IBuildProcessor
    {
        [MoonAssetBuildMethod(5, "PackVFiles", tooltip: "打包vfs资源，见实现IVFilePacker接口的类")]
        private static void PackVFiles()
        {
            IVFilePacker.PackVFiles();
        }

        [MoonAssetBuildMethod(10, "ApplyBuildGroups", false, tooltip: "应用 BuildGroups 配置，生成资源打包清单")]
        private static void ApplyBuildGroups()
        {
            BuildScript.ApplyBuildGroups();
        }

        [MoonAssetBuildMethod(20, "Build AssetBundles", false, tooltip: "打ab、rawfile")]
        private static void BuildAssetBundles()
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            BuildScript.BuildAssetBundles();

            BuildScript.AppendBundleHash();

            watch.Stop();

            AddManfestVersion();
        }

        private static void AddManfestVersion()
        {
            var manifest = BuildScript.GetManifest();
            var buildGroups = BuildScript.GetBuildGroups();
            var resVersion = buildGroups.AddResVersion();
            manifest.appVersion = Application.version;
            manifest.resVersion = resVersion;
            Manifest.Build(manifest);
        }

        [MoonAssetBuildMethod(44, "Upload Assets to FileServer(based on Manifest)", false, tooltip: "根据打包后的manifest文件，将资源上传到服务器，资源服url在manifest里配置")]
        private static void UploadAssetsToFileServerUseManifest()
        {
            var folderToUpload = MoonAssetConfig.k_Editor_DlcOutputPath;
            if (!Directory.Exists(folderToUpload)) return;

            var localFiles = new List<string>();
            var remoteFiles = new List<string>();

            var manifestPath = folderToUpload + "/" + MoonAssetConfig.k_ManifestAsset;
            var manifest = Manifest.Create(manifestPath);
            if (manifest == null)
            {
                Log.ERROR($"load manifest error: {manifestPath}");
                return;
            }

            var url = manifest.remoteAssetUrl;
            var remoteAssets = manifest.RemoteAssets;

            foreach (var item in remoteAssets)
            {
                var name = item.Value.Name;

                localFiles.Add(folderToUpload + "/" + name);
                remoteFiles.Add(MoonAssetConfig.GetRemoteAssetURL(null, name));
            }

            localFiles.Add(manifestPath);
            remoteFiles.Add(MoonAssetConfig.GetRemoteAssetURL(null, MoonAssetConfig.k_ManifestAsset));

            void OnProgress(string fileName, int count, int length)
            {
                EditorUtility.DisplayProgressBar("Upload", fileName, count / (float)length);
            }

            OnProgress("", 0, 1);
            Saro.Net.Http.HttpHelper.UploadFiles(url, localFiles, remoteFiles, OnProgress);

            UnityEngine.Object.DestroyImmediate(manifest);
            GC.Collect();
        }

        [MoonAssetBuildMethod(45, "Copy to DLC Folder", false, tooltip: "根据 BuildGroups 的 BuiltIn标记 将DLC目录指定资源拷贝到StreammingAssets")]
        private static void CopyDlcFolderToStreammingAssets()
        {
            var sb = new StringBuilder(102400);

            var manifest = BuildScript.GetManifest();
            manifest.Init();

            var group = BuildScript.GetBuildGroups();

            var destFolder = Application.streamingAssetsPath + "/" + MoonAssetConfig.k_Dlc + "/" + MoonAssetConfig.GetCurrentPlatformName();

            if (Directory.Exists(destFolder))
            {
                Directory.Delete(destFolder, true);
            }

            if (!Directory.Exists(destFolder))
            {
                Directory.CreateDirectory(destFolder);
            }

            if (!Directory.Exists(MoonAssetConfig.k_Editor_DlcOutputPath)) return;

            // bundle
            var builtInBundles = group.GetBuiltInBundles(manifest);
            sb.AppendLine("** builtInAssets");
            sb.AppendLine("*bundle:" + builtInBundles.Length);
            foreach (var fileName in builtInBundles)
            {
                var src = MoonAssetConfig.k_Editor_DlcOutputPath + "/" + fileName;
                var dest = Path.Combine(destFolder, fileName);

                var directory = Path.GetDirectoryName(dest);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (File.Exists(src))
                {
                    File.Copy(src, dest, true);
                    sb.Append(src).AppendLine();
                }
                else
                {
                    throw new Exception("CopyDlcFolderToStreammingAssets. bundle not found: " + src);
                }
            }

            // manifest
            sb.AppendLine();
            sb.AppendLine("*manifest:");
            var manifestPath = MoonAssetConfig.k_Editor_DlcOutputPath + "/" + MoonAssetConfig.k_ManifestAsset;
            if (File.Exists(manifestPath))
            {
                var dest = destFolder + "/" + MoonAssetConfig.k_ManifestAsset;
                File.Copy(manifestPath, dest);
                sb.Append(manifestPath).AppendLine();
            }
            else
            {
                throw new Exception("CopyDlcFolderToStreammingAssets. manifest not found: " + manifestPath);
            }

            Log.ERROR(sb.ToString());

            // build indexes copy完成之后，做一次
            FileUtility.BuildIndexes();

            AssetDatabase.Refresh();
        }

        [MoonAssetBuildMethod(50, "Build Player", false, tooltip: "打包")]
        private static void BuildPlayer()
        {
            //FileUtility.BuildIndexes();

            BuildScript.BuildPlayer();
        }
    }
}