﻿using Saro.Attributes;
using Saro.Pool;
using Saro.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

//sbp 不支持变体
//https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@1.5/manual/UpgradeGuide.html

namespace Saro.MoonAsset.Build
{
    public partial class BuildGroups : ScriptableObject
    {
        #region BundleGroup

        private readonly Dictionary<string, string> m_Asset2Bundles = new Dictionary<string, string>(1024, StringComparer.Ordinal);
        private readonly Dictionary<string, string[]> m_ConflictedAssets = new Dictionary<string, string[]>(1024, StringComparer.Ordinal);
        private readonly HashSet<string> m_NeedOptimizedAssets = new HashSet<string>();
        private readonly Dictionary<string, HashSet<string>> m_Tracker = new Dictionary<string, HashSet<string>>(1024, StringComparer.Ordinal);

        [Header("Settings")]
        [Tooltip("是否用hash代替bundle名称")]
        public bool nameBundleByHash = true;

        [Tooltip("TODO 在asset名字后面，补上文件hash，避免cdn缓存问题")]
        public bool appendAssetHash = true;

        [Tooltip("不被打包的资源，全小写")]
        public List<string> excludeAssets = new List<string>()
        {
            ".meta",
            ".dll",
            ".cs",
            ".js",
            ".boo",
            ".spriteatlas",
            ".spriteatlasv2",
            ".giparams",
            "lightingdata.asset",
        };

        [Tooltip("构建的版本号")]
        [Header("Builds")]
        public int resVersion = 0;

        [HideInInspector]
        public int resVersionBuildAsset;

        [Tooltip("built-in场景")]
        public SceneAsset[] scenesInBuild = new SceneAsset[0];

        [Header("AssetBundle打包")]
        public BundleGroup[] bundleGroups = new BundleGroup[0];

        [ReadOnly]
        public RuleAsset[] ruleAssets = new RuleAsset[0];

        [ReadOnly]
        public RuleBundle[] ruleBundles = new RuleBundle[0];

        #region API

        public void ApplyResVersionBuildAsset()
        {
            resVersionBuildAsset = resVersion;
        }

        public bool IsNeedAddResVersion()
        {
            return resVersionBuildAsset == resVersion;
        }

        /// <summary>
        /// 更新资源版本号
        /// </summary>
        /// <returns></returns>
        public int AddResVersion()
        {
            //var versionObj = new Version(version);
            //var revision = versionObj.Revision + 1;
            //versionObj = new Version(versionObj.Major, versionObj.Minor, versionObj.Build, revision);
            //version = versionObj.ToString();
            ++resVersion;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            return resVersion;
        }

        public void Apply()
        {
            Clear();

            CollectAssets();
            AnalysisAssets();
            OptimizeAssets();

            ProcessSpriteAtlases();

            Save();

            ApplyRawGroups();
        }

        public AssetBundleBuild[] GetAssetBundleBuilds()
        {
            var builds = new AssetBundleBuild[ruleBundles.Length];
            for (int i = 0; i < ruleBundles.Length; i++)
            {
                RuleBundle ruleBundle = ruleBundles[i];
                builds[i] = new AssetBundleBuild
                {
                    assetNames = ruleBundle.assets,
                    assetBundleName = ruleBundle.bundle,

                    // if use short path
                    //addressableNames = ruleBundle.assets.Select(Path.GetFileNameWithoutExtension).ToArray()
                };
            }

            return builds;
        }

        #endregion

        #region Private

        internal static bool ValidateAsset(string asset)
        {
            // unity资源只有 assets/ packages/ 这俩目录可以被打包
            if (!(asset.StartsWith("Assets/") || asset.StartsWith("Packages/")))
            {
                Debug.LogError($"invalid asset: {asset}");
                return false;
            }

            // 文件夹也跳过
            if (Directory.Exists(asset)) return false;

            var fileName = Path.GetFileName(asset).ToLower();

            var buildGroups = BuildScript.GetBuildGroups();
            var excludeAssets = buildGroups.excludeAssets;
            foreach (var item in excludeAssets)
            {
                if (fileName.EndsWith(item))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool IsSceneAsset(string asset)
        {
            return asset.EndsWith(".unity");
        }

        internal static bool IsShaderAsset(string asset)
        {
            return asset.EndsWith(".shader") || asset.EndsWith(".shadervariants");
        }

        internal string RuledAssetBundleName(string asset)
        {
            if (string.IsNullOrEmpty(asset)) throw new ArgumentNullException("asset");

            if (nameBundleByHash)
            {
                return Utility.HashUtility.GetMd5HexHash(asset) + MoonAssetConfig.k_AssetExtension;
            }
            else
            {
                var newName = asset + MoonAssetConfig.k_AssetExtension;
                newName.ReplaceFast('/', '_').ReplaceFast('\\', '_').ToLowerFast();
                return newName;
            }
        }

        private void Track(string asset, string bundle)
        {
            if (!m_Tracker.TryGetValue(asset, out HashSet<string> bundles))
            {
                bundles = new HashSet<string>();
                m_Tracker.Add(asset, bundles);
            }

            bundles.Add(bundle);

            // 一个asset在多个bundles里, 即冗余了
            if (bundles.Count > 1)
            {
                m_Asset2Bundles.TryGetValue(asset, out string bundleName);
                if (string.IsNullOrEmpty(bundleName))
                {
                    m_NeedOptimizedAssets.Add(asset);
                }
            }
        }

        private Dictionary<string, List<string>> GetBundle2Assets()
        {
            var assetCount = m_Asset2Bundles.Count;
            var bundles = new Dictionary<string, List<string>>(assetCount / 2, StringComparer.Ordinal);
            foreach (var item in m_Asset2Bundles)
            {
                var bundle = item.Value;
                if (!bundles.TryGetValue(bundle, out List<string> list))
                {
                    list = new List<string>(64);
                    bundles[bundle] = list;
                }

                var asset = item.Key;
                if (!list.Contains(asset)) list.Add(asset);
            }

            return bundles;
        }

        private void Clear()
        {
            m_Tracker.Clear();
            m_NeedOptimizedAssets.Clear();
            m_ConflictedAssets.Clear();
            m_Asset2Bundles.Clear();
        }

        private void Save()
        {
            var array = new RuleAsset[m_Asset2Bundles.Count];
            int index = 0;
            foreach (var item in m_Asset2Bundles)
            {
                array[index++] = new RuleAsset
                {
                    asset = item.Key,
                    bundle = item.Value
                };
            }

            Array.Sort(array, (a, b) => string.Compare(a.asset, b.asset, StringComparison.Ordinal));
            ruleAssets = array;

            var bundle2Assets = GetBundle2Assets();
            ruleBundles = new RuleBundle[bundle2Assets.Count];
            var i = 0;
            foreach (var item in bundle2Assets)
            {
                ruleBundles[i] = new RuleBundle
                {
                    bundle = item.Key,
                    assets = item.Value.ToArray()
                };
                i++;
            }

            EditorUtility.ClearProgressBar();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        private void OptimizeAssets()
        {
            // 剔除冗余资源
            int i = 0, max = m_ConflictedAssets.Count;
            foreach (var item in m_ConflictedAssets)
            {
                if (EditorUtility.DisplayCancelableProgressBar($"优化冲突{i}/{max}", item.Key, i / (float) max))
                    break;

                var list = item.Value;
                foreach (string asset in list)
                {
                    if (!IsSceneAsset(asset))
                        m_NeedOptimizedAssets.Add(asset);
                }

                i++;
            }

            i = 0;
            max = m_NeedOptimizedAssets.Count;
            foreach (var item in m_NeedOptimizedAssets)
            {
                if (EditorUtility.DisplayCancelableProgressBar($"优化冗余{i}/{max}", item, i / (float) max))
                    break;

                OptimizeAsset(item);
                i++;
            }
        }

        private void AnalysisAssets()
        {
            var bundle2Assets = GetBundle2Assets();
            int i = 0, max = bundle2Assets.Count;
            foreach (var item in bundle2Assets)
            {
                var bundle = item.Key;

                if (EditorUtility.DisplayCancelableProgressBar($"分析依赖{i}/{max}", bundle, i / (float) max))
                    break;

                var assetPaths = bundle2Assets[bundle];

                var pathNames = assetPaths.ToArray();

                bool hasScene = false;
                bool allScene = true;
                foreach (string asset in assetPaths)
                {
                    if (IsSceneAsset(asset))
                    {
                        hasScene = true;
                    }
                    else
                    {
                        allScene = false;

                        if (IsShaderAsset(asset))
                            m_NeedOptimizedAssets.Add(asset);
                    }
                }

                bool sceneBundleConflicted = hasScene && !allScene;
                if (sceneBundleConflicted)
                    m_ConflictedAssets.Add(bundle, pathNames);

                // 获取所有被引用的资源
                var dependencies = AssetDatabase.GetDependencies(pathNames, true);
                if (dependencies.Length > 0)
                {
                    // 获取所有冗余项
                    foreach (var asset in dependencies)
                    {
                        if (ValidateAsset(asset))
                        {
                            Track(asset, bundle);

                            if (IsShaderAsset(asset))
                                m_NeedOptimizedAssets.Add(asset);
                        }
                    }
                }

                i++;
            }
        }

        private void CollectAssets()
        {
            for (int i = 0, max = bundleGroups.Length; i < max; i++)
            {
                var group = bundleGroups[i];

                if (EditorUtility.DisplayCancelableProgressBar($"收集资源{i}/{max}", group.searchPath, i / (float) max))
                    break;

                ApplyBundleGroup(group);
            }
        }

        private void OptimizeAsset(string asset)
        {
            if (IsShaderAsset(asset))
                m_Asset2Bundles[asset] = RuledAssetBundleName("shaders");
            else
                m_Asset2Bundles[asset] = RuledAssetBundleName(asset);
        }

        private void ApplyBundleGroup(BundleGroup group)
        {
            var assets = group.GetAssets();

            foreach (var asset in assets)
            {
                if (IsShaderAsset(asset))
                {
                    m_Asset2Bundles[asset] = RuledAssetBundleName("shaders");
                    continue;
                }

                switch (group.nameBy)
                {
                    case BundleGroup.ENameBy.Path:
                    {
                        m_Asset2Bundles[asset] = RuledAssetBundleName(asset);
                        break;
                    }
                    case BundleGroup.ENameBy.Directory:
                    {
                        m_Asset2Bundles[asset] = RuledAssetBundleName(Path.GetDirectoryName(asset).ReplaceFast('\\', '/'));
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        #region SpriteAtlas

        private Dictionary<string, HashSet<string>> m_SpriteAtlasTracker = new Dictionary<string, HashSet<string>>();

        private void ProcessSpriteAtlases()
        {
            m_SpriteAtlasTracker.Clear();

            var atlasPaths = Directory.GetFiles("Assets/", "*.spriteatlas", SearchOption.AllDirectories);
            for (int i = 0; i < atlasPaths.Length; i++)
            {
                string atlasPath = atlasPaths[i];
                if (EditorUtility.DisplayCancelableProgressBar($"处理图集{i}/{atlasPaths.Length}", atlasPath, i / (float) atlasPaths.Length))
                    break;

                var bundleName = RuledAssetBundleName(atlasPath);

                foreach (var assetPath in AssetDatabase.GetDependencies(atlasPath))
                {
                    if (!ValidateAsset(assetPath))
                    {
                        continue;
                    }

                    if (!m_SpriteAtlasTracker.TryGetValue(assetPath, out var bundles))
                    {
                        bundles = new HashSet<string>();
                        m_SpriteAtlasTracker.Add(assetPath, bundles);
                    }

                    bundles.Add(bundleName);

                    if (bundles.Count > 1)
                        Log.ERROR($"ProcessSpriteAtlases. {assetPath} duplicated at [{string.Join(",", bundles)}] {bundleName}");
                }
            }

            foreach (var item in m_SpriteAtlasTracker)
            {
                var assetPath = item.Key;
                var bundleName = item.Value.First();

                // 将散图打进图集包
                m_Asset2Bundles[assetPath] = bundleName;
            }
        }

        #endregion

        #endregion

        #region AssetsMap

#if UNITY_EDITOR
        internal Dictionary<string, string> Asset2BundleCahce
        {
            get
            {
                if (m_Asset2BundleCahce == null)
                {
                    m_Asset2BundleCahce = new Dictionary<string, string>();

                    foreach (var vbundle in ruleBundles)
                    {
                        foreach (var vasset in vbundle.assets)
                        {
                            m_Asset2BundleCahce.Add(vasset, vbundle.bundle);
                        }
                    }
                }

                return m_Asset2BundleCahce;
            }
            set { m_Asset2BundleCahce = value; }
        }

        private Dictionary<string, string> m_Asset2BundleCahce;

        internal string[] GetAllAssetBundleNames()
        {
            return Array.ConvertAll(ruleBundles, bundle => bundle.bundle);
        }

        internal string GetAssetBundleName(string assetPath)
        {
            // sbp 不支持变体！
            return GetImplicitAssetBundleName(assetPath);
        }

        internal string GetImplicitAssetBundleName(string assetPath)
        {
            if (Asset2BundleCahce.TryGetValue(assetPath, out var bundle))
            {
                return bundle;
            }

            return null;
        }

        internal string[] GetAssetPathsFromAssetBundle(string assetBundleName)
        {
            foreach (var vbundle in ruleBundles)
            {
                if (string.CompareOrdinal(vbundle.bundle, assetBundleName) == 0)
                    return vbundle.assets;
            }

            return new string[0];
        }
#endif

        #endregion

        #endregion

        #region RawGroup

        [Header("RawAsset打包")]
        public RawGroup[] rawGroups = new RawGroup[0];

        private void ApplyRawGroups()
        {
            foreach (var group in rawGroups)
            {
                var assets = group.GetAssets();
            }
        }

        #endregion

        #region MyRegion

        /// <summary>
        /// 获取包体资源，需要打完包才行
        /// </summary>
        /// <returns></returns>
        public string[] GetBuiltInAssetBundles(Manifest manifest)
        {
            using (HashSetPool<string>.Rent(out var set))
            {
                foreach (var group in bundleGroups)
                {
                    if (group.builtIn)
                    {
                        var bundles = group.GetAssetBundles(manifest);
                        foreach (var bundle in bundles)
                        {
                            set.Add(bundle);
                        }
                    }
                }

                return set.ToArray();
            }
        }

        /// <summary>
        /// 获取包体资源，需要打完包才行
        /// </summary>
        /// <returns></returns>
        public string[] GetBuiltInRawAssets(Manifest manifest)
        {
            using (HashSetPool<string>.Rent(out var set))
            {
                foreach (var group in rawGroups)
                {
                    if (group.builtIn)
                    {
                        var name = group.GetCustomBundleName(manifest);
                        if (!string.IsNullOrEmpty(name))
                            set.Add(name);

                        /*
                        if (group.packAsVFS)
                        {
                            set.Add(XAssetPath.k_CustomFolder + "/" + group.groupName);
                        }
                        else
                        {
                            foreach (var asset in group.assets)
                            {
                                set.Add(XAssetPath.k_CustomFolder + "/" + group.groupName + "/" + asset.asset);
                            }
                        }
                        */
                    }
                }

                return set.ToArray();
            }
        }

        #endregion
    }
}