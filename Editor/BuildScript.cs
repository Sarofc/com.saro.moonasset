using Saro.IO;
using Saro.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Pipeline;
using UnityEngine;

namespace Saro.MoonAsset.Build
{
    /*
     * 需要切换到当前需要打包的平台，然后才能打包
     */
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

            var outputFolder = MoonAssetConfig.k_Editor_BuildOutputPath;

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

            // 配置宏定义
            //var (overrideSymbols, originalSymbols) = BeginOverrideSymbols();

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = builtInScenes,
                locationPathName = outputFolder + "/" + targetAppName,

                target = EditorUserBuildSettings.activeBuildTarget,
            };

            if (EditorUserBuildSettings.development)
            {
                buildPlayerOptions.options |= BuildOptions.Development;
            }

            if (GetSettings().detailBuildReport)
            {
                buildPlayerOptions.options |= BuildOptions.DetailedBuildReport;
            }

            BuildPipeline.BuildPlayer(buildPlayerOptions);

            //EndOverrideSymbols(overrideSymbols, originalSymbols);

            //Utility.OpenFolderUtility.OpenDirectory(outputFolder);
            //Debug.LogError("Open Folder: " + outputFolder);
        }

        public static void BuildAssetBundles()
        {
            //var (overrideSymbols, originalSymbols) = BeginOverrideSymbols();

            BuildAssetBundles_SBP();

            //EndOverrideSymbols(overrideSymbols, originalSymbols);
        }

        public static void AppendBundleHash()
        {
            var buildGroups = BuildScript.GetBuildGroups();
            if (buildGroups.appendAssetHash)
            {
                var directory = MoonAssetConfig.k_Editor_DlcOutputPath;

                var manifest = BuildScript.GetManifest();

                // bundle append hash
                foreach (var item in manifest.bundles)
                {
                    string fileName = item.name;
                    var newName = MoonAssetConfig.AppendHashToFileName(fileName, item.hash);

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

        private static void BuildAssetBundles_SBP()
        {
            var outputFolder = MoonAssetConfig.k_Editor_DlcOutputPath;

            if (Directory.Exists(outputFolder))
                Directory.Delete(outputFolder, true);

            Directory.CreateDirectory(outputFolder);

            // 1. build assetbundle
            var options = GetSettings().buildAssetBundleOptions;
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildGroups = GetBuildGroups();
            var assetBundleBuilds = buildGroups.GetAssetBundleBuilds();

            var retCode = BuildAssetBundle_SBP.BuildAssetBundles(outputFolder, assetBundleBuilds, options, buildTarget,
                out var result);

            if (retCode != ReturnCode.Success)
            {
                Debug.LogError("Build AssetBundle Error. code: " + retCode);
                return;
            }

            //// sbpmanifest 测试
            //var sbpManifest = ScriptableObject.CreateInstance<CompatibilityAssetBundleManifest>();
            //sbpManifest.SetResults(result.BundleInfos);
            //File.WriteAllText(outputFolder, JsonUtility.ToJson(sbpManifest));
            //MoonAsset.ERROR(JsonUtility.ToJson(sbpManifest));

            // =================
            // 2. build rawbundle
            var rawBundles = buildGroups.GetRawBundleBuilds();
            foreach (var item in rawBundles)
            {
                var bundle = item.bundle;

                var src = item.assets[0];
                var dst = string.Format("{0}/{1}", outputFolder, bundle);

                // copy to
                File.Copy(src, dst, true);
            }
            // =================

            // 3. create manifest
            var bundleInfos = result.BundleInfos; // ab
            var allBundles = bundleInfos.Keys.ToList();
            var manifest = GetManifest();
            var dirs = new List<string>();
            var bundle2Ids = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var item in rawBundles) // raw
                allBundles.Add(item.bundle);

            for (int index = 0; index < allBundles.Count; index++)
            {
                bundle2Ids[allBundles[index]] = index;
            }

            var bundleRefs = new List<BundleRef>(allBundles.Count);
            for (var index = 0; index < allBundles.Count; index++)
            {
                var bundle = allBundles[index];

                string[] deps = null;
                if (bundleInfos.TryGetValue(bundle, out var bundleDetails))
                    deps = bundleInfos[bundle].Dependencies;

                var path = string.Format("{0}/{1}", outputFolder, bundle);
                if (File.Exists(path))
                {
                    using (var fs = File.OpenRead(path))
                    {
                        bundleRefs.Add(new BundleRef
                        {
                            name = bundle,
                            deps = deps != null ? Array.ConvertAll(deps, input => bundle2Ids[input]) : null,
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

            var assetRefs = new List<AssetRef>(buildGroups.ruleAssets.Length);
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
                        name = Path.GetFileName(path),
                        bundle = bundle2Ids[item.bundle],
                        dir = index,
                    };
                    assetRefs.Add(asset);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{item.bundle} {index} {Path.GetFileName(path)}");
                    throw e;
                }
            }

            // =================
            // spriteatlas
            var ruleSprites = buildGroups.ruleSprites;
            var atlasRefs = new List<SpriteAtlasRef>(ruleSprites.Length);
            for (int i = 0; i < ruleSprites.Length; i++)
            {
                var ruleSprite = ruleSprites[i];
                var atlasRef = new SpriteAtlasRef();

                {
                    var spritePath = ruleSprite.spritePath;
                    var dirSprite = Path.GetDirectoryName(spritePath).Replace("\\", "/");
                    var indexSprite = dirs.FindIndex(o => o.Equals(dirSprite));
                    if (indexSprite == -1)
                    {
                        indexSprite = dirs.Count;
                        dirs.Add(dirSprite);
                    }

                    atlasRef.sprite = Path.GetFileName(spritePath);
                    atlasRef.dirSprite = indexSprite;
                }

                {
                    var atlasPath = ruleSprite.atlasPath;
                    var dirAtlas = Path.GetDirectoryName(atlasPath).Replace("\\", "/");
                    var indexAtlas = dirs.FindIndex(o => o.Equals(dirAtlas));
                    if (indexAtlas == -1)
                    {
                        indexAtlas = dirs.Count;
                        dirs.Add(dirAtlas);
                    }

                    atlasRef.atlas = Path.GetFileName(atlasPath);
                    atlasRef.dirAtlas = indexAtlas;
                }

                atlasRefs.Add(atlasRef);
            }
            // =================

            manifest.dirs = dirs.ToArray();
            manifest.assets = assetRefs.ToArray();
            manifest.bundles = bundleRefs.ToArray();
            manifest.atlases = atlasRefs.ToArray();

            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
            return GetAsset<Manifest>(MoonAssetConfig.k_Editor_ManifestAssetPath);
        }

        internal static BuildGroups GetBuildGroups()
        {
            return GetAsset<BuildGroups>(MoonAssetConfig.k_Editor_BuildGroupsPath);
        }

        internal static Settings GetSettings()
        {
            return GetAsset<Settings>(MoonAssetConfig.k_Editor_SettingsPath);
        }


        // TODO 覆盖宏功能，需要更多测试，先关掉。
        // 打 代码、ab、包体，可能存在代码不统一的问题
        private static int s_OverrideSymbolsIndex = 0;
        // 开始覆盖宏
        public static (bool overrideSymbols, string[] originalSymbols) BeginOverrideSymbols()
        {
            if (s_OverrideSymbolsIndex++ > 0)
            {
                s_OverrideSymbolsIndex = 0;
                Log.ERROR("BeginOverrideSymbols/EndOverrideSymbols 必须成对出现");
            }

            var namedBuildTarget =
                NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            var overrideSymbols = GetSettings().overrideSymbols;
            if (overrideSymbols)
            {
                PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out var originalSymbols);
                var newSymbols = GetSettings().scriptingDefineSymbols;
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, newSymbols);
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();

                Log.INFO($"BeginOverrideSymbols. Set: {string.Join(";", newSymbols)}");

                return (overrideSymbols, originalSymbols);
            }

            return (overrideSymbols, null);
        }

        // 还原宏
        public static void EndOverrideSymbols(bool overrideSymbols, string[] originalSymbols)
        {
            if (overrideSymbols)
            {
                var namedBuildTarget =
                    NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, originalSymbols);

                Log.INFO($"EndOverrideSymbols. Rest: {string.Join(";", originalSymbols)}");
            }

            s_OverrideSymbolsIndex--;
        }
    }
}