using Saro.Attributes;
using Saro.Pool;
using Saro.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

//sbp 不支持变体
//https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@1.5/manual/UpgradeGuide.html

namespace Saro.MoonAsset.Build
{
    public partial class BuildGroups : ScriptableObject
    {
        private readonly Dictionary<string, string> m_Asset2Bundles = new(1024, StringComparer.Ordinal);
        private readonly HashSet<string> m_RawFileSet = new(1024, StringComparer.Ordinal);
        private readonly Dictionary<string, string[]> m_ConflictedAssets = new(1024, StringComparer.Ordinal);
        private readonly HashSet<string> m_NeedOptimizedAssets = new();
        private readonly Dictionary<string, HashSet<string>> m_Tracker = new(1024, StringComparer.Ordinal);

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
            ".giparams",
            "lightingdata.asset",

            //".spriteatlas",
            //".spriteatlasv2",
        };

        [Tooltip("构建的版本号")]
        [Header("Builds")]
        public int resVersion = 0;

        [Tooltip("built-in场景")]
        public SceneAsset[] scenesInBuild = new SceneAsset[0];

        [Header("AssetBundle打包")]
        public BundleGroup[] bundleGroups = new BundleGroup[0];

        [ReadOnly]
        public RuleAsset[] ruleAssets = new RuleAsset[0];

        [ReadOnly]
        public RuleBundle[] ruleBundles = new RuleBundle[0];

        [ReadOnly]
        public RuleSprite[] ruleSprites = new RuleSprite[0];

        #region API

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

            // 处理unity资源
            CollectAssets();
            AnalysisAssets();
            OptimizeAssets();

            // 处理unity图集
            ProcessSpriteAtlases();

            // 再保存
            Save();
        }

        public AssetBundleBuild[] GetAssetBundleBuilds()
        {
            var builds = new List<AssetBundleBuild>(ruleBundles.Length);
            for (int i = 0; i < ruleBundles.Length; i++)
            {
                var ruleBundle = ruleBundles[i];

                if (ruleBundle.isRawFile) continue;

                builds.Add(new AssetBundleBuild
                {
                    assetNames = ruleBundle.assets,
                    assetBundleName = ruleBundle.bundle,

                    // if use short path
                    //addressableNames = ruleBundle.assets.Select(Path.GetFileNameWithoutExtension).ToArray()
                });
            }

            return builds.ToArray();
        }

        public RuleBundle[] GetRawBundleBuilds()
        {
            var builds = new List<RuleBundle>(ruleBundles.Length);
            for (int i = 0; i < ruleBundles.Length; i++)
            {
                var ruleBundle = ruleBundles[i];

                if (ruleBundle.isRawFile)
                {
                    builds.Add(ruleBundle);
                }
            }

            return builds.ToArray();
        }

        #endregion

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

            //var fileName = Path.GetFileName(asset).ToLower();
            var fileName = Path.GetFileName(asset);

            var buildGroups = BuildScript.GetBuildGroups();
            var excludeAssets = buildGroups.excludeAssets;
            foreach (var item in excludeAssets)
            {
                if (fileName.EndsWith(item, StringComparison.OrdinalIgnoreCase))
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

        internal string RuledBundleName(string name, bool raw = false)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("asset");

            var extension = raw ? MoonAssetConfig.k_RawAssetExtension : MoonAssetConfig.k_AssetExtension;
            if (nameBundleByHash)
            {
                if (appendAssetHash) // 如果appendhash，则使用短hash
                    return Utility.HashUtility.GetCrc32HexHash(name) + extension;
                else
                    return Utility.HashUtility.GetMd5HexHash(name) + extension;
            }
            else
            {
                var newName = name + MoonAssetConfig.k_AssetExtension;
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
            m_RawFileSet.Clear();
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
                    assets = item.Value.ToArray(),
                    isRawFile = IsRawFile(item.Key),
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
                if (EditorUtility.DisplayCancelableProgressBar($"优化冲突{i}/{max}", item.Key, i / (float)max))
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
                if (EditorUtility.DisplayCancelableProgressBar($"优化冗余{i}/{max}", item, i / (float)max))
                    break;

                OptimizeAsset(item);
                i++;
            }
        }

        private bool IsRawFile(string bundle)
        {
            return m_RawFileSet.Contains(bundle);
        }

        private void AnalysisAssets()
        {
            var bundle2Assets = GetBundle2Assets();
            int i = 0, max = bundle2Assets.Count;
            foreach (var item in bundle2Assets)
            {
                var bundle = item.Key;

                // raw file 不需要进行资源检测
                if (IsRawFile(bundle)) continue;

                if (EditorUtility.DisplayCancelableProgressBar($"分析依赖{i}/{max}", bundle, i / (float)max))
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

                if (EditorUtility.DisplayCancelableProgressBar($"收集资源{i}/{max}", group.searchPath, i / (float)max))
                    break;

                ApplyBundleGroup(group);
            }
        }

        private void OptimizeAsset(string asset)
        {
            if (IsShaderAsset(asset))
                m_Asset2Bundles[asset] = RuledBundleName("shaders");
            else
                m_Asset2Bundles[asset] = RuledBundleName(asset);
        }

        private void ApplyBundleGroup(BundleGroup group)
        {
            var assets = group.GetAssets();

            foreach (var asset in assets)
            {
                if (IsShaderAsset(asset))
                {
                    m_Asset2Bundles[asset] = RuledBundleName("shaders");
                    continue;
                }

                string bundleName = null;
                switch (group.packedBy)
                {
                    case BundleGroup.EPackedBy.File:
                        {
                            bundleName = RuledBundleName(asset);
                            break;
                        }
                    case BundleGroup.EPackedBy.RawFile:
                        {
                            bundleName = RuledBundleName(asset, true);
                            break;
                        }
                    case BundleGroup.EPackedBy.Directory:
                        {
                            bundleName = RuledBundleName(Path.GetDirectoryName(asset).ReplaceFast('\\', '/'));
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                m_Asset2Bundles[asset] = bundleName;

                if (group.IsRawFile)
                {
                    m_RawFileSet.Add(bundleName);
                }
            }
        }

        #region SpriteAtlas

        /*
         *
         * sbp模式，只打了 atlas，且勾选include，sprite散图不打进ab，首包场景里没有引用图集的散图，结果只有 ab 里有一张图集，没有散图，此结果是正确的。
         *
         * 1. 由于散图不参与打包，且上层逻辑希望直接通过散图路径加载，所以需要包装一层，见SpriteAtlasRef。
         * 2. 多个预制体，引用一个图集，也是没问题的。
         * 3. 如果首包场景里引用到了 分包的图集，那么首包里会冗余一张图集，没有散图。
         *
         */

        private readonly Dictionary<string, HashSet<string>> m_SpriteAtlasTracker = new(128);
        private readonly Dictionary<string, string> m_SpriteToAtlas = new(128);

        private void ProcessSpriteAtlases()
        {
            // 保险起见，确保图集引用正确。因为图集改动后，spriteatlas引用不会立刻生效
            SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);

            m_SpriteAtlasTracker.Clear();
            m_SpriteToAtlas.Clear();

            var atlasPaths = m_Asset2Bundles.Keys.ToArray();
            for (int i = 0; i < atlasPaths.Length; i++)
            {
                string atlasPath = atlasPaths[i];

                if (!atlasPath.EndsWith(".spriteatlas")) continue;

                if (EditorUtility.DisplayCancelableProgressBar($"处理图集{i}/{atlasPaths.Length}", atlasPath, i / (float)atlasPaths.Length))
                    break;

                var bundleName = RuledBundleName(atlasPath);

                foreach (var assetPath in AssetDatabase.GetDependencies(atlasPath))
                {
                    if (assetPath.EndsWith(".spriteatlas")) continue;

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

                    m_SpriteToAtlas.Add(assetPath, atlasPath); // 记录sprite到atlas的查找表
                }
            }

            foreach (var item in m_SpriteAtlasTracker)
            {
                var assetPath = item.Key;
                //var bundleName = item.Value.First();

                m_Asset2Bundles.Remove(assetPath); // 在图集里的散图，不要参与打包

                //Log.ERROR($"remove sprite from atlas: {assetPath}");
            }

            ruleSprites = m_SpriteToAtlas.Select(x => new RuleSprite { spritePath = x.Key, atlasPath = x.Value }).ToArray();
        }

        // 适用于 legacy 打包管线，spriteatlas不打包，散图打成一个ab
        private void ProcessSpriteAtlases_Legacy()
        {
            m_SpriteAtlasTracker.Clear();

            var atlasPaths = Directory.GetFiles("Assets/", "*.spriteatlas", SearchOption.AllDirectories);
            for (int i = 0; i < atlasPaths.Length; i++)
            {
                string atlasPath = atlasPaths[i];
                if (EditorUtility.DisplayCancelableProgressBar($"处理图集{i}/{atlasPaths.Length}", atlasPath, i / (float)atlasPaths.Length))
                    break;

                var bundleName = RuledBundleName(atlasPath);

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

                //将散图打进图集包
                m_Asset2Bundles[assetPath] = bundleName;
            }
        }

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

        /// <summary>
        /// 获取包体资源，需要打完包才行
        /// </summary>
        /// <returns></returns>
        public string[] GetBuiltInBundles(Manifest manifest)
        {
            using (HashSetPool<string>.Rent(out var set))
            {
                foreach (var group in bundleGroups)
                {
                    if (group.builtIn)
                    {
                        var bundles = group.GetBundles(manifest);
                        foreach (var bundle in bundles)
                        {
                            set.Add(bundle);
                        }
                    }
                }

                return set.ToArray();
            }
        }
    }
}