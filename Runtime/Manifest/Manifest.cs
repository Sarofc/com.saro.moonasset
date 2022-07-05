using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Saro.MoonAsset
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

        #region RawBundles

        [Header("RawBundles")]

        public string[] rawDirs = new string[0];

        /// <summary>
        /// 非ab的其他自定义资源，例如数据表什么的
        /// </summary>
        public AssetRef[] rawAssets = new AssetRef[0];

        /// <summary>
        /// 非ab的其他自定义资源bundle
        /// <code>用FileIO来读取</code>
        /// <code>这是使用自定义构建来处理吧</code>
        /// </summary>
        public RawBundleRef[] rawBundles = new RawBundleRef[0];

        public IReadOnlyDictionary<string, RawBundleRef> RawAssetMap => m_RawAssetMap;
        private Dictionary<string, RawBundleRef> m_RawAssetMap;

        /// <summary>
        /// 用于appendhash后，通过简易名称获取完整名称，eg. cards -> cards_xxxxxxxxxxx
        /// <code>TODO 不够完备，可能会重名</code>
        /// </summary>
        /// <param name="singleName"></param>
        /// <returns></returns>
        public string GetFullRawBundleName(string singleName)
        {
            for (int i = 0; i < rawBundles.Length; i++)
            {
                var bundle = rawBundles[i];
                if (bundle.name.StartsWith(singleName))
                {
                    return bundle.name;
                }
            }

            return null;
        }

        #endregion

        #region SpriteAtlas

        [Header("SpriteToAtlas")]
        public SpriteAtlasRef[] atlases = new SpriteAtlasRef[0];

        #endregion

        /// <summary>
        /// 运行时 对比表
        /// [IRemoteAssets.Name, IRemoteAssets]
        /// </summary>
        public IReadOnlyDictionary<string, IRemoteAssets> RemoteAssets => m_RemoteAssets;
        private Dictionary<string, IRemoteAssets> m_RemoteAssets;

        /// <summary>
        /// spriteatlas查找表，可以直接使用sprite路径加载，上层可以对图集无感知
        /// </summary>
        public Dictionary<string, string> SpriteToAtlas => m_SpriteToAtlas;
        private Dictionary<string, string> m_SpriteToAtlas;

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

            // raw bundle
            if (m_RawAssetMap != null && rawAssets.Length <= m_RawAssetMap.Count)
            {
                m_RawAssetMap.Clear();
            }
            else
            {
                m_RawAssetMap = new Dictionary<string, RawBundleRef>(rawAssets.Length);
            }
            foreach (var item in rawAssets)
            {
                var path = string.Format("{0}/{1}", rawDirs[item.dir], item.name);
                if (item.bundle >= 0 && item.bundle < rawBundles.Length)
                {
                    m_RawAssetMap[path] = rawBundles[item.bundle];
                }
                else
                {
                    Log.ERROR(string.Format("{0} raw bundle index {1} not exist.", path, item.bundle));
                }
            }

            // remote assets
            if (m_RemoteAssets != null && bundles.Length + rawBundles.Length <= m_RemoteAssets.Count)
            {
                m_RemoteAssets.Clear();
            }
            else
            {
                m_RemoteAssets = new Dictionary<string, IRemoteAssets>(bundles.Length + rawBundles.Length, StringComparer.OrdinalIgnoreCase);
            }

            for (int i = 0; i < bundles.Length; i++)
            {
                var bundle = bundles[i];
                m_RemoteAssets.Add(((IRemoteAssets)bundle).Name, bundle);
            }

            for (int i = 0; i < rawBundles.Length; i++)
            {
                var rawBundle = rawBundles[i];
                m_RemoteAssets.Add(((IRemoteAssets)rawBundle).Name, rawBundle);
            }

            // spriteatlas
            if (m_SpriteToAtlas != null && m_SpriteToAtlas.Count <= atlases.Length) m_SpriteToAtlas.Clear();
            else m_SpriteToAtlas = new Dictionary<string, string>(atlases.Length);

            for (int i = 0; i < atlases.Length; i++)
            {
                var item = atlases[i];
                var spritePath = string.Format("{0}/{1}", dirs[item.dirSprite], item.sprite);
                var atlasPath = string.Format("{0}/{1}", dirs[item.dirAtlas], item.atlas);
                m_SpriteToAtlas.Add(spritePath, atlasPath);
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

            this.rawDirs = other.rawDirs;
            this.rawAssets = other.rawAssets;
            this.rawBundles = other.rawBundles;

            this.m_AssetToBundle = other.m_AssetToBundle;
            this.m_BundleToDeps = other.m_BundleToDeps;

            this.m_RawAssetMap = other.m_RawAssetMap;
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

        private bool ValidRawAsset(string path)
        {
            if (path.EndsWith(".dump")) return false;
            if (path.EndsWith(".dump.json")) return false;
            if (path.EndsWith(".dump.txt")) return false;
            if (path.EndsWith(".meta")) return false;

            // add more

            return true;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Build(Manifest manifest)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(manifest);
            UnityEditor.AssetDatabase.SaveAssets();

            var manifestPath = $"{MoonAssetConfig.k_Editor_DlcOutputPath}/{MoonAssetConfig.k_ManifestAsset}";
            var manifestJson = JsonUtility.ToJson(manifest);
            File.WriteAllText(manifestPath, manifestJson);
#endif
        }
    }
}