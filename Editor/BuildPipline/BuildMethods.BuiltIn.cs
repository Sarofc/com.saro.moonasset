﻿using Saro.Utility;
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
        [MoonAssetBuildMethod(-2, "ClearAssetBundleNames", false)]
        private static void ClearAssetBundles()
        {
            BuildScript.ClearAssetBundleNames();
        }

        //[XAssetBuildMethod(-1, "Mark AssetBundleNames", false)]
        //private static void MarkAssetBundleNames()
        //{
        //    XAssetBuildScript.MarkAssetBundleNames();
        //}

        [MoonAssetBuildMethod(0, "ApplyBuildGroups", false)]
        private static void ApplyBuildGroups()
        {
            BuildScript.ApplyBuildGroups();
        }

        //[XAssetBuildMethod(15, "打包数据表")]
        [System.Obsolete("使用RawAssets", true)]
        private static void PackTables()
        {
            try
            {
                // 拷贝数据表到 Extra 里
                {
                    var tablePath = "tables/data/config";
                    var files = Directory.GetFiles(tablePath, "*", SearchOption.AllDirectories);

                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);
                        var dst = MoonAssetConfig.k_Editor_DlcOutputPath + "/" + MoonAssetConfig.k_RawFolder + "/" + fileName;

                        File.Copy(file, dst, true);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[XAsset] 打包数据表 error:" + e);
                return;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MoonAssetBuildMethod(20, "Build AssetBundles", false)]
        private static void BuildAssetBundles()
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            BuildScript.BuildAssetBundles();

            BuildScript.AppendBundleHash();

            watch.Stop();

            var buildGroups = BuildScript.GetBuildGroups();
            buildGroups.ApplyResVersionBuildAsset();
        }


        [MoonAssetBuildMethod(40, "Build RawBundles", tooltip = "非AB资源打包成vfs文件")]
        private static void BuildRawBundles()
        {
            BuildScript.BuildRawBundles();

            BuildScript.AppendRawBundleHash();

            var buildGroups = BuildScript.GetBuildGroups();
            buildGroups.ApplyResVersionBuildAsset();
        }

        [MoonAssetBuildMethod(41, "Add ManfestVersion")]
        private static void AddManfestVersion()
        {
            var manifest = BuildScript.GetManifest();
            var buildGroups = BuildScript.GetBuildGroups();
            var resVersion = buildGroups.AddResVersion();
            manifest.appVersion = Application.version;
            manifest.resVersion = resVersion;
            Manifest.Build(manifest);
        }

        [MoonAssetBuildMethod(44, "Upload Assets to FileServer(based on Manifest)", false)]
        private static void UploadAssetsToFileServerUseManifest()
        {
            var buildGroups = BuildScript.GetBuildGroups();
            if (buildGroups.IsNeedAddResVersion())
            {
                if (EditorUtility.DisplayDialog("错误", "重新打过资源，上传前，需要先 AddManfestVersion", "好的", "我再想想"))
                {
                    AddManfestVersion();
                }
                else
                {
                    return;
                }
            }

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

            // TODO filter file

            void OnProgress(string fileName, int count, int length)
            {
                EditorUtility.DisplayProgressBar("Upload", fileName, count / (float)length);
            }

            OnProgress("", 0, 1);
            Saro.Net.Http.HttpHelper.UploadFiles(url, localFiles, remoteFiles, OnProgress);

            UnityEngine.Object.DestroyImmediate(manifest);
            GC.Collect();
        }

        [MoonAssetBuildMethod(45, "Copy to DLC Folder", false)]
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
            var builtInBundles = group.GetBuiltInAssetBundles(manifest);
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

            // custom asset
            var builtInCustomAssets = group.GetBuiltInRawAssets(manifest);
            sb.AppendLine();
            sb.AppendLine("*custom asset:" + builtInCustomAssets.Length);
            foreach (var fileName in builtInCustomAssets)
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
                    throw new Exception("CopyDlcFolderToStreammingAssets. assest not found: " + src);
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

            AssetDatabase.Refresh();
        }

        [MoonAssetBuildMethod(50, "Build Player", false)]
        private static void BuildPlayer()
        {
            FileUtility.BuildIndexes();
            BuildScript.BuildPlayer();
        }
    }
}