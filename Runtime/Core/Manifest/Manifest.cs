using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Saro.XAsset
{

    public class Manifest : ScriptableObject
    {
        public const int k_Version = 1;
        /// <summary>
        /// Manifest自己的版本
        /// </summary>
        [Saro.Attributes.ReadOnly]
        public int version = k_Version;
        /// <summary>
        /// 游戏版本
        /// </summary>
        public string appVersion;
        /// <summary>
        /// 资源版本
        /// </summary>
        public int resVersion;
        /// <summary>
        /// [可选] 远程版本文件的完整路径，用来判断服务器端是否有新版本的资源
        /// </summary>
        public string remoteVersionUrl = "";
        /// <summary>
        /// 远程资源 文件的前缀路径，一般直接填cdn了
        /// </summary>
        public string remoteAssetUrl = "";

        #region AssetBundle

        [Header("AssetBundle")]
        /// <summary>
        /// 路径
        /// </summary>
        public string[] dirs = new string[0];

        /// <summary>
        /// 资源
        /// </summary>
        public AssetRef[] assets = new AssetRef[0];

        /// <summary>
        /// AB包
        /// </summary>
        public BundleRef[] bundles = new BundleRef[0];

        /// <summary>
        /// 资源路径表
        /// [AssetPath, BundleRef.Name]
        /// </summary>
        public IReadOnlyDictionary<string, BundleRef> AssetToBundle => m_AssetToBundle;

        /// <summary>
        /// AssetBundle依赖表
        /// [BundleRef.Name, BundleRef[]]
        /// </summary>
        public IReadOnlyDictionary<string, BundleRef[]> BundleToDeps => m_BundleToDeps;

        private Dictionary<string, BundleRef> m_AssetToBundle;

        private Dictionary<string, BundleRef[]> m_BundleToDeps;

        #endregion

        #region CustomAssets

        [Header("CustomAssets")]

        public string[] customDirs = new string[0];

        /// <summary>
        /// 非ab的其他自定义资源，例如数据表什么的
        /// </summary>
        public AssetRef[] customAssets = new AssetRef[0];

        /// <summary>
        /// 非ab的其他自定义资源bundle
        /// <code>用FileIO来读取</code>
        /// <code>这是使用自定义构建来处理吧</code>
        /// </summary>
        public CustomBundleRef[] customBundles = new CustomBundleRef[0];

        public IReadOnlyDictionary<string, CustomBundleRef> CustomAssetMap => m_CustomAssetMap;
        private Dictionary<string, CustomBundleRef> m_CustomAssetMap;

        /// <summary>
        /// 用于appendhash后，通过简易名称获取完整名称，eg. cards -> cards_xxxxxxxxxxx
        /// </summary>
        /// <param name="singleName"></param>
        /// <returns></returns>
        public string GetFullCustomBundleName(string singleName)
        {
            for (int i = 0; i < customBundles.Length; i++)
            {
                var bundle = customBundles[i];
                if (bundle.name.StartsWith(singleName))
                {
                    return bundle.name;
                }
            }

            return null;
        }

        #endregion

        /// <summary>
        /// 运行时 对比表
        /// [IRemoteAssets.Name, IRemoteAssets]
        /// </summary>
        public IReadOnlyDictionary<string, IRemoteAssets> RemoteAssets => m_RemoteAssets;

        private Dictionary<string, IRemoteAssets> m_RemoteAssets;

        public void Load(string content)
        {
            JsonUtility.FromJsonOverwrite(content, this);

            Init();
        }

        public void Init()
        {
            // asset bundle
            if (m_AssetToBundle != null && assets.Length <= m_AssetToBundle.Count)
            {
                m_AssetToBundle.Clear();
            }
            else
            {
                m_AssetToBundle = new Dictionary<string, BundleRef>(assets.Length, StringComparer.OrdinalIgnoreCase);
            }

            if (m_BundleToDeps != null && bundles.Length <= m_BundleToDeps.Count)
            {
                m_BundleToDeps.Clear();
            }
            else
            {
                m_BundleToDeps = new Dictionary<string, BundleRef[]>(bundles.Length, StringComparer.OrdinalIgnoreCase);
            }

            foreach (var item in assets)
            {
                var path = string.Format("{0}/{1}", dirs[item.dir], item.name);
                if (item.bundle >= 0 && item.bundle < bundles.Length)
                {
                    m_AssetToBundle[path] = bundles[item.bundle];
                }
                else
                {
                    Log.ERROR(string.Format("{0} asset bundle index {1} not exist.", path, item.bundle));
                }
            }

            for (int i = 0; i < bundles.Length; i++)
            {
                BundleRef item = bundles[i];
                m_BundleToDeps[item.name] = Array.ConvertAll(item.deps, id => bundles[id]);
            }

            // custom bundle
            if (m_CustomAssetMap != null && customAssets.Length <= m_CustomAssetMap.Count)
            {
                m_CustomAssetMap.Clear();
            }
            else
            {
                m_CustomAssetMap = new Dictionary<string, CustomBundleRef>(customAssets.Length);
            }
            foreach (var item in customAssets)
            {
                var path = string.Format("{0}/{1}", customDirs[item.dir], item.name);
                if (item.bundle >= 0 && item.bundle < customBundles.Length)
                {
                    m_CustomAssetMap[path] = customBundles[item.bundle];
                }
                else
                {
                    Log.ERROR(string.Format("{0} custom bundle index {1} not exist.", path, item.bundle));
                }
            }

            // remote assets
            if (m_RemoteAssets != null && bundles.Length + customBundles.Length <= m_RemoteAssets.Count)
            {
                m_RemoteAssets.Clear();
            }
            else
            {
                m_RemoteAssets = new Dictionary<string, IRemoteAssets>(bundles.Length + customBundles.Length, StringComparer.OrdinalIgnoreCase);
            }

            for (int i = 0; i < bundles.Length; i++)
            {
                var bundle = bundles[i];
                m_RemoteAssets.Add(((IRemoteAssets)bundle).Name, bundle);
            }

            for (int i = 0; i < customBundles.Length; i++)
            {
                var customBundle = customBundles[i];
                m_RemoteAssets.Add(((IRemoteAssets)customBundle).Name, customBundle);
            }
        }

        public bool TryGetRemoteAsset(string name, out IRemoteAssets remoteAsset)
        {
            return m_RemoteAssets.TryGetValue(name, out remoteAsset);
        }

        public void Override(Manifest other, string path)
        {
            this.version = other.version;
            this.appVersion = other.appVersion;
            this.resVersion = other.resVersion;
            this.remoteVersionUrl = other.remoteVersionUrl;
            this.remoteAssetUrl = other.remoteAssetUrl;

            this.dirs = other.dirs;
            this.assets = other.assets;
            this.bundles = other.bundles;

            this.customDirs = other.customDirs;
            this.customAssets = other.customAssets;
            this.customBundles = other.customBundles;

            this.m_AssetToBundle = other.m_AssetToBundle;
            this.m_BundleToDeps = other.m_BundleToDeps;

            this.m_CustomAssetMap = other.m_CustomAssetMap;
            this.m_RemoteAssets = other.m_RemoteAssets;

            var json = JsonUtility.ToJson(this);

            File.WriteAllText(path, json);
        }

        public static Manifest Create(string path)
        {
            var manifest = ScriptableObject.CreateInstance<Manifest>();
            var content = File.ReadAllText(path);
            manifest.Load(content);
            return manifest;
        }

        public static IEnumerable<IRemoteAssets> Diff(Manifest local, Manifest remote)
        {
            if (remote == null) return null;
            if (local == null) return remote.m_RemoteAssets.Values;

            //if (local.resVersion == remote.resVersion) return null;

            var diff = new List<IRemoteAssets>();
            foreach (var kv in remote.m_RemoteAssets)
            {
                if (!local.m_RemoteAssets.TryGetValue(kv.Key, out var asset))
                {
                    diff.Add(kv.Value);
                }
                else
                {
                    if (string.Compare(asset.Hash, kv.Value.Hash, StringComparison.OrdinalIgnoreCase) != 0)
                        diff.Add(kv.Value);
                }
            }
            return diff;
        }

        public bool IsValid()
        {
            return version == k_Version;
        }

        public override string ToString()
        {
#if UNITY_EDITOR
            return JsonUtility.ToJson(this);
#else
            return base.ToString();
#endif
        }

        //        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        //        public void AddCustomAssets(IEnumerable<string> assetPaths)
        //        {
        //#if UNITY_EDITOR
        //            foreach (var assetPath in assetPaths)
        //            {
        //                AddCustomAsset(assetPath);
        //            }
        //#endif
        //        }

        //        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        //        public void AddCustomAsset(string assetPath)
        //        {
        //#if UNITY_EDITOR
        //            if (!ValidCustomAsset(assetPath)) return;

        //            var fileInfo = new FileInfo(assetPath);
        //            if (fileInfo.Exists)
        //            {
        //                var asset = new CustomBundleRef
        //                {
        //                    name = XAssetPath.k_CustomFolder + "/" + fileInfo.Name,
        //                    size = fileInfo.Length,
        //                };


        //                using (var fs = fileInfo.OpenRead())
        //                {
        //                    asset.hash = HashUtility.GetMd5Hash(fs);
        //                    //Log.ERROR(extraAsset.ToString());
        //                }

        //                customBundles.Add(asset);

        //                UnityEditor.EditorUtility.SetDirty(this);
        //            }
        //            else
        //            {
        //                Log.ERROR("File Not Found: " + assetPath);
        //            }
        //#endif
        //        }

        private bool ValidCustomAsset(string path)
        {
            if (path.EndsWith(".dump")) return false;
            if (path.EndsWith(".dump.json")) return false;
            if (path.EndsWith(".dump.txt")) return false;
            if (path.EndsWith(".meta")) return false;

            // add more

            return true;
        }

        //#if UNITY_EDITOR
        //        [ContextMenu("Build")]
        //        public void Build()
        //        {
        //            UnityEditor.EditorUtility.SetDirty(this);
        //            UnityEditor.AssetDatabase.Refresh();
        //            Build(this);
        //        }
        //#endif

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Build(Manifest manifest)
        {
#if UNITY_EDITOR
            var manifestPath = $"{XAssetConfig.k_Editor_DlcOutputPath}/{XAssetConfig.k_ManifestAsset}";
            var manifestJson = JsonUtility.ToJson(manifest);
            File.WriteAllText(manifestPath, manifestJson);
#endif
        }
    }
}