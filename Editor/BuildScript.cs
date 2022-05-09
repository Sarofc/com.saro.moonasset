
using Saro.IO;
using Saro.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEngine;

namespace Saro.XAsset.Build
{
    public static class BuildScript
    {
        public static void ClearAssetBundleNames()
        {
            var allAssetBundleNames = AssetDatabase.GetAllAssetBundleNames();
            for (var i = 0; i < allAssetBundleNames.Length; i++)
            {
                var assetBundleName = allAssetBundleNames[i];
                if (EditorUtility.DisplayCancelableProgressBar(
                                    string.Format("Clear AssetBundles {0}/{1}", i, allAssetBundleNames.Length), assetBundleName,
                                    i * 1f / allAssetBundleNames.Length))
                    break;

                AssetDatabase.RemoveAssetBundleName(assetBundleName, true);
            }
            EditorUtility.ClearProgressBar();
        }

        public static void MarkAssetBundleNames()
        {
            ClearAssetBundleNames();

            var buildGroups = GetBuildGroups();
            for (int i = 0; i < buildGroups.ruleBundles.Length; i++)
            {
                var bundle = buildGroups.ruleBundles[i];
                for (int j = 0; j < bundle.assets.Length; j++)
                {
                    var asset = bundle.assets[j];
                    var importer = AssetImporter.GetAtPath(asset);
                    importer.assetBundleName = bundle.bundle;
                }
            }

            AssetDatabase.RemoveUnusedAssetBundleNames();
        }

        internal static void ApplyBuildGroups()
        {
            var rules = GetBuildGroups();
            rules.Apply();
        }

        private static string[] GetBuiltInScenesFromSettings()
        {
            var builtInScenes = GetBuildGroups().scenesInBuild;
            var scenes = new HashSet<string>();
            foreach (SceneAsset item in builtInScenes)
            {
                var path = AssetDatabase.GetAssetPath(item);
                if (!string.IsNullOrEmpty(path))
                {
                    scenes.Add(path);
                }
            }

            return scenes.ToArray();
        }

        [System.Obsolete("Legacy Build, use SBP now", true)]
        private static string GetAssetBundleManifestFilePath()
        {
            //var relativeAssetBundlesOutputPathForPlatform = Path.Combine("Asset", GetPlatformName());
            //return Path.Combine(relativeAssetBundlesOutputPathForPlatform, GetPlatformName()) + ".manifest";
            return null;
        }

        public static void BuildPlayer()
        {
            var targetAppName = GetBuildTargetAppName(EditorUserBuildSettings.activeBuildTarget);

            var outputFolder = XAssetConfig.k_Editor_BuildOutputPath;

            if (outputFolder.Length == 0)
                return;

            var builtInScenes = GetBuiltInScenesFromSettings();
            if (builtInScenes.Length == 0)
            {
                Debug.LogError("Built In Scenes is empty. Nothing to build!");
                return;
            }

            if (targetAppName == null)
                return;

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = builtInScenes,
                locationPathName = outputFolder + "/" + targetAppName,

                // TODO 配置宏定义 看看是什么机制再说
                //extraScriptingDefines = GetSettings().ExtraScriptingDefines,

                target = EditorUserBuildSettings.activeBuildTarget,
            };

            if (EditorUserBuildSettings.development)
            {
                buildPlayerOptions.options |= BuildOptions.Development;
            }

            if (GetSettings().detailBuildReport)
            {
#if UNITY_2020_1_OR_NEWER
                buildPlayerOptions.options |= BuildOptions.DetailedBuildReport;
#endif
            }

            BuildPipeline.BuildPlayer(buildPlayerOptions);

            //Utility.OpenFolderUtility.OpenDirectory(outputFolder);
            //Debug.LogError("Open Folder: " + outputFolder);
        }

        public static void BuildAssetBundles()
        {
            SBPBuildAssetBundles();
        }

        public static void AppendBundleHash()
        {
            var buildGroups = BuildScript.GetBuildGroups();
            if (buildGroups.appendAssetHash)
            {
                var directory = XAssetConfig.k_Editor_DlcOutputPath;

                var manifest = BuildScript.GetManifest();

                // bundle append hash
                foreach (var item in manifest.bundles)
                {
                    // TODO test
                    string fileName = item.name;
                    var newName = XAssetConfig.AppendHashToFileName(fileName, item.hash);

                    item.name = newName;

                    var oldFilePath = directory + "/" + fileName;
                    var newFilePath = directory + "/" + newName;

                    File.Delete(newFilePath);
                    File.Move(oldFilePath, newFilePath);
                }

                EditorUtility.SetDirty(manifest);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static void SBPBuildAssetBundles()
        {
            var outputFolder = XAssetConfig.k_Editor_DlcOutputPath + "/" + XAssetConfig.k_AssetBundleFoler;

            if (Directory.Exists(outputFolder))
                Directory.Delete(outputFolder, true);

            Directory.CreateDirectory(outputFolder);

            var options = GetSettings().buildAssetBundleOptions;

            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildGroups = GetBuildGroups();
            var assetBundleBuilds = buildGroups.GetAssetBundleBuilds();

            var retCode = SBPBuildAssetBundle.BuildAssetBundles(outputFolder, assetBundleBuilds, options, buildTarget, out var result);

            if (retCode != ReturnCode.Success)
            {
                Debug.LogError("Build AssetBundle Error. code: " + retCode);
                return;
            }

            //// sbpmanifest 测试
            //var sbpManifest = ScriptableObject.CreateInstance<CompatibilityAssetBundleManifest>();
            //sbpManifest.SetResults(result.BundleInfos);
            //File.WriteAllText(outputFolder, JsonUtility.ToJson(sbpManifest));
            //XAssetComponent.ERROR(JsonUtility.ToJson(sbpManifest));

            var bundleInfos = result.BundleInfos;
            var manifest = GetManifest();
            var dirs = new List<string>();
            var assetRefs = new List<AssetRef>();
            var bundles = bundleInfos.Keys.ToArray();
            var bundle2Ids = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int index = 0; index < bundles.Length; index++)
            {
                var bundle = bundles[index];
                bundle2Ids[bundle] = index;
            }

            var bundleRefs = new List<BundleRef>();
            for (var index = 0; index < bundles.Length; index++)
            {
                var bundle = bundles[index];
                var deps = bundleInfos[bundle].Dependencies;
                var path = string.Format("{0}/{1}", outputFolder, bundle);
                if (File.Exists(path))
                {
                    using (var fs = File.OpenRead(path))
                    {
                        bundleRefs.Add(new BundleRef
                        {
                            name = XAssetConfig.k_AssetBundleFoler + "/" + bundle,
                            deps = Array.ConvertAll(deps, input => bundle2Ids[input]),
                            size = fs.Length,
                            hash = HashUtility.GetMd5HexHash(fs), // 改用自己的md5hash
                            //hash = assetBundleManifest[bundle].Hash.ToString(),
                        });
                    }
                }
                else
                {
                    Debug.LogError(path + " file not exsit.");
                }
            }

            for (var i = 0; i < buildGroups.ruleAssets.Length; i++)
            {
                var item = buildGroups.ruleAssets[i];
                var path = item.asset;
                var dir = Path.GetDirectoryName(path).Replace("\\", "/");

                var index = dirs.FindIndex(o => o.Equals(dir));
                if (index == -1)
                {
                    index = dirs.Count;
                    dirs.Add(dir);
                }
                try
                {
                    var asset = new AssetRef
                    {
                        bundle = bundle2Ids[item.bundle],
                        dir = index,
                        name = Path.GetFileName(path),
                    };
                    assetRefs.Add(asset);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{item.bundle} {index} {Path.GetFileName(path)}");
                    throw e;
                }
            }

            manifest.dirs = dirs.ToArray();
            manifest.assets = assetRefs.ToArray();
            manifest.bundles = bundleRefs.ToArray();

            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void BuildRawBundles()
        {
            try
            {
                EditorUtility.DisplayProgressBar("BuildCustomAssets", "start build", 0);

                var buildGroups = GetBuildGroups();
                var customGroups = buildGroups.rawGroups;

                var outputDirectory = XAssetConfig.k_Editor_DlcOutputPath + "/" + XAssetConfig.k_RawFolder;
                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, true);
                }

                Directory.CreateDirectory(outputDirectory);

                var dirs = new List<string>();
                var assetRefs = new List<AssetRef>();
                var bundleRefs = new List<RawBundleRef>();

                for (int k = 0; k < customGroups.Length; k++)
                {
                    RawGroup group = customGroups[k];
                    var assets = group.assets;

                    var dst = outputDirectory + "/" + group.groupName;

                    var directory = Path.GetDirectoryName(dst);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    if (File.Exists(dst))
                    {
                        File.Delete(dst);
                    }

                    if (assets.Count > 0)
                    {
                        using (var vfs = VFileSystem.Open(dst, FileMode.CreateNew, FileAccess.Write, assets.Count, assets.Count))
                        {
                            for (int i = 0; i < assets.Count; i++)
                            {
                                var asset = assets[i];
                                var fileName = asset.name;

                                EditorUtility.DisplayProgressBar("BuildCustomAssets", $"pack {group.groupName}/{fileName}", ((i + 1) / (float)assets.Count));

                                var dir = Path.GetDirectoryName(fileName).Replace("\\", "/");
                                dir = string.IsNullOrEmpty(dir) ? group.groupName : group.groupName + "/" + dir;
                                var index = dirs.FindIndex(o => o.Equals(dir));
                                if (index == -1)
                                {
                                    index = dirs.Count;
                                    dirs.Add(dir);
                                }

                                if (!group.disableAssetName)
                                {
                                    assetRefs.Add(new AssetRef
                                    {
                                        name = Path.GetFileName(fileName),
                                        dir = index,
                                        bundle = k,
                                    });
                                }

                                var src = group.searchPaths[asset.dir] + "/" + fileName;
                                var name = group.groupName + "/" + fileName;
                                vfs.WriteFile(name, src);
                            }

                            Log.ERROR(string.Join(", ", vfs.GetAllFileInfos()));
                            Log.ERROR("vfs file count:" + vfs.FileCount);
                        }

                        using (var fs = File.OpenRead(dst))
                        {
                            bundleRefs.Add(new RawBundleRef
                            {
                                name = XAssetConfig.k_RawFolder + "/" + group.groupName,
                                size = fs.Length,
                                hash = HashUtility.GetMd5HexHash(fs),
                            });
                        }
                    }
                }

                var manifest = BuildScript.GetManifest();
                manifest.rawDirs = dirs.ToArray();
                manifest.rawAssets = assetRefs.ToArray();
                manifest.rawBundles = bundleRefs.ToArray();

                EditorUtility.SetDirty(manifest);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Log.ERROR(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static void AppendRawBundleHash()
        {
            var buildGroups = BuildScript.GetBuildGroups();
            if (buildGroups.appendAssetHash)
            {
                var directory = XAssetConfig.k_Editor_DlcOutputPath;

                var manifest = BuildScript.GetManifest();

                // customAssets append hash
                foreach (var item in manifest.rawBundles)
                {
                    // TODO test
                    var fileName = item.name;
                    var newName = XAssetConfig.AppendHashToFileName(fileName, item.hash);

                    item.name = newName;

                    var oldFilePath = directory + "/" + fileName;
                    var newFilePath = directory + "/" + newName;

                    File.Delete(newFilePath);
                    File.Move(oldFilePath, newFilePath);
                }

                EditorUtility.SetDirty(manifest);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        public static string GetBuildTargetAppName(BuildTarget target)
        {
            string name = PlayerSettings.productName;
            string version = "v" + Application.version + "." + GetBuildGroups().resVersion;

            switch (target)
            {
                case BuildTarget.Android:
                    return string.Format("{0}-{1}.apk", name, version);

                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return string.Format("{1}/{0}.exe", name, version);

                //case BuildTarget.StandaloneOSX:
                //case BuildTarget.iOS:
                //case BuildTarget.WebGL:
                //    return "";

                // Add more build targets for your own.
                default:
                    Debug.Log("Target not implemented.");
                    return null;
            }
        }

        internal static T GetAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
            }

            return asset;
        }

        public static Manifest GetManifest()
        {
            return GetAsset<Manifest>(XAssetConfig.k_Editor_ManifestAssetPath);
        }

        internal static BuildGroups GetBuildGroups()
        {
            return GetAsset<BuildGroups>(XAssetConfig.k_Editor_BuildGroupsPath);
        }

        internal static Settings GetSettings()
        {
            return GetAsset<Settings>(XAssetConfig.k_Editor_SettingsPath);
        }
    }
}