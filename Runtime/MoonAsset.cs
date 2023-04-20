//#define DEBUG_MOONASSET

using Saro.Core;
using Saro.Net;
using Saro.Utility;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

namespace Saro.MoonAsset
{
    /*
     *
     * https://baddogzz.github.io/2020/02/07/Unload-Resources/
     *
     * WARN
     *
     * 1. 如果资源加载没控制好，需要在合适的时候调用 Resources.UnloadUnusedAssets()
     * 
     * INFO
     * 
     * 1. webgl 走 indexes，BundleLocator_Local(streamingAssets)  判定为本地存在，然后判断为http链接，走 BundleRequest_UWR
     *
     */

    public sealed partial class MoonAsset : IAssetManager
    {
        public Action<string, bool> OnLoadRemoteAsset { get; set; }

        public Action<string> OnLoadRemoteAssetError { get; set; } = (assetName) =>
        {
            ERROR($" <color=blue>[auto]</color> OnLoadRemoteAssetError. download error. assetName: {assetName}");
        };

        /// <summary>
        /// 当前资源清单
        /// </summary>
        public Manifest Manifest => m_Manifest;
        private Manifest m_Manifest;

        /// <summary>
        /// TODO 当前manifest指向的远端资源版本地址
        /// </summary>
        public string RemoteVersionUrl => m_Manifest != null ? m_Manifest.remoteVersionUrl : null;

        // <summary>
        /// 当前manifest指向的远端资源地址
        /// </summary>
        public string RemoteAssetUrl => m_Manifest != null ? m_Manifest.remoteAssetUrl : null;

        public class MoonAssetPolicy
        {
            /// <summary>
            /// 自动卸载资源，默认关闭
            /// </summary>
            public bool AutoUnloadAsset { get; set; } = false;

            /// <summary>
            /// <see cref="AutoUnloadAsset"/>开启后，多久调用一次<see cref="UnloadUnusedAssets"/>
            /// </summary>
            public float AutoUnloadAssetInterval { get; set; } = 5f;

            /// <summary>
            /// 卸载场景时，卸载无用资源，默认开启
            /// </summary>
            public bool UnloadAssetWhenUnloadScene { get; set; } = true;
        }

        public MoonAssetPolicy Policy { get; private set; } = new MoonAssetPolicy();

        private float m_TimeToAutoUnloadAsset;

        public enum EMode
        {
            [Tooltip("使用 AssetDatabase，仅限编辑器")]
            AssetDatabase = 0,

            [Tooltip("使用 AssetBundle，加载 ExtraAssets 目录")]
            Simulate = 1,

            [Tooltip("使用 AssetBundle，真机模式")]
            Runtime = 2,
        }

        public static EMode s_Mode = EMode.AssetDatabase;

        /// <summary>
        /// 只内部调用，外部通过 <see cref="IAssetManager.Current"/> 来获取
        /// </summary>
        internal static MoonAsset Current => Main.Resolve<IAssetManager>() as MoonAsset;

        #region Service Impl

        void IService.Update()
        {
            //UnityEngine.Profiling.Profiler.BeginSample("[MoonAsset] UpdateAssets");
            UpdateAssets();
            //UnityEngine.Profiling.Profiler.EndSample();

            //UnityEngine.Profiling.Profiler.BeginSample("[MoonAsset] UpdateBundles");
            UpdateBundles();
            //UnityEngine.Profiling.Profiler.EndSample();

            //UnityEngine.Profiling.Profiler.BeginSample("[MoonAsset] AutoUnloadUnusedAssets");
            AutoUnloadUnusedAssets();
            //UnityEngine.Profiling.Profiler.EndSample();
        }

        void IService.Awake() { }

        void IService.Dispose() { }

        #endregion

        #region API

        internal async Task InitializeAsync()
        {
#if !UNITY_EDITOR
            s_Mode = EMode.Runtime;
#endif

            INFO($"AssetMode = {s_Mode}");

            //ClearAssetReference();

            var manifest = await LoadLocalManifest(MoonAssetConfig.k_ManifestAsset);
            if (manifest != null)
            {
                OnManifestLoaded(manifest);
            }
        }

        /*
             issue 进游戏热更后
            从没有appendhash，转为appendhash后，因载入了部分内容相同，但名字不一样的ab
            报错 The AssetBundle 'xxx.bundle' can't be loaded because another AssetBundle with the same files is already loaded.

            方案1
            AssetBundle.UnloadAllAssetBundles(false); 此种方式，一些旧bundle加载出来的资源，就没有被卸载掉，需要调用 Resources.UnloadUnusedAssets() 清理;
            
            方案2
            区分 可热更bundle，这类bundle在资源加载完成前，不要加载

            方案3
            那不能被清理得资源要标记上 HideFlags.DontUnloadUnusedAsset?未试验过是否可行，仅提供一个方向
        */
        /// <summary>
        /// 清理所有资源引用
        /// <code>WARN 此接口可能会导致一些问题，谨慎调用</code>
        /// </summary>
        /// <param name="unloadAllObjects">强制卸载所有资源，only for AssetBundle</param>
        public void ClearAssetReference(bool unloadAllObjects = true)
        {
            // 大致同方案1，清理掉ref，使用unload(false)
            //AssetBundle.UnloadAllAssetBundles(unloadAllObjects);

            INFO("<color=red>ClearAssetReference</color>");

            foreach (var item in m_RequestMap)
            {
                item.Value.SetRefCountForce(0);
            }

            foreach (var item in m_BundleRequestMap)
            {
                item.Value.SetRefCountForce(0);
            }

            UnloadUnusedAssets();
            UpdateAssets();
            UpdateBundles(unloadAllObjects);
        }

        public string GetAppVersion()
        {
            if (m_Manifest == null) return "-1";
            return m_Manifest.appVersion;
        }

        public string GetResVersion()
        {
            if (m_Manifest == null) return "-1";
            return m_Manifest.resVersion.ToString();
        }

        private SceneRequest m_MainSceneRequest;

        public IAssetHandle LoadSceneAsync(string path, bool additive = false)
        {
            if (string.IsNullOrEmpty(path))
            {
                ERROR("invalid path");
                return null;
            }

            var request = new SceneRequest(path, additive);
            if (!additive)
            {
                if (m_MainSceneRequest != null)
                {
                    m_MainSceneRequest.DecreaseRefCount();
                    m_MainSceneRequest = null;
                }

                m_MainSceneRequest = request;
            }

            request.Load();
            request.IncreaseRefCount();
            m_SceneRequests.Add(request);

            __AnalyzeRequest(request);

            INFO($"LoadScene: {path}");

            return request;
        }

        public IAssetHandle LoadAssetAsync(string path, Type type)
        {
            return LoadAssetAsyncInternal(path, type);
        }

        public IAssetHandle LoadAsset(string path, Type type)
        {
            var request = LoadAssetAsyncInternal(path, type);
            request.WaitForCompletion();
            return request;
        }

        /// <summary>
        /// 卸载所有无用资源，只计算出哪些资源是无用的，并没有真正卸载
        /// </summary>
        public void UnloadUnusedAssets(bool immediate = true)
        {
            // asset
            foreach (var item in m_RequestMap)
            {
                if (item.Value.IsUnused())
                {
                    m_UnusedRequests.Add(item.Value);
                }
            }

            foreach (var request in m_UnusedRequests)
            {
                m_RequestMap.Remove(request.AssetUrl);
            }

            // bundle
            foreach (var item in m_BundleRequestMap)
            {
                if (item.Value.IsUnused())
                {
                    m_UnusedBundleRequests.Add(item.Value);
                }
            }

            foreach (var request in m_UnusedBundleRequests)
            {
                m_BundleRequestMap.Remove(request.AssetUrl);
            }

            INFO($"<color=red>UnloadUnusedAssets.</color>");
        }

        public void DeleteDLC()
        {
            if (System.IO.Directory.Exists(MoonAssetConfig.k_DlcPath))
            {
                try
                {
                    System.IO.Directory.Delete(MoonAssetConfig.k_DlcPath, true);
                }
                catch (Exception e)
                {
                    ERROR(e.ToString());
                }
            }
        }

        #endregion

        #region Private

        internal async Task<Manifest> LoadLocalManifest(string manifestName)
        {
#if UNITY_EDITOR
            if (s_Mode == EMode.AssetDatabase)
            {
                return null;
            }
#endif

            if (TryGetAssetPath(manifestName, out var manifestPath, out _))
            {
                string content = await FileUtility.ReadAllTextAsync(manifestPath);

                if (!string.IsNullOrEmpty(content))
                {
                    var manifest = ScriptableObject.CreateInstance<Manifest>();
                    manifest.Load(content);
                    return manifest;
                }
            }

            return null;
        }

        private void OnManifestLoaded(Manifest manifest)
        {
            if (m_Manifest != null)
            {
                GameObject.Destroy(m_Manifest);
            }

            m_Manifest = manifest;

            //INFO(m_Manifest.ToString());
        }

        private void AutoUnloadUnusedAssets()
        {
            if (Policy.AutoUnloadAsset)
            {
                if (m_TimeToAutoUnloadAsset < Time.unscaledTime)
                {
                    UnloadUnusedAssets(false);

                    m_TimeToAutoUnloadAsset = Time.unscaledTime + Policy.AutoUnloadAssetInterval;
                }
            }
        }

        internal readonly Dictionary<string, Request> m_RequestMap = new(StringComparer.Ordinal); // 已加载asset
        internal readonly List<Request> m_LoadingRequests = new(128); // 正在加载asset
        internal readonly List<Request> m_UnusedRequests = new(128); // 待卸载asset
        internal readonly List<SceneRequest> m_SceneRequests = new(4); // 正在加载/已加载 scene

        private void UpdateAssets()
        {
            for (var i = 0; i < m_LoadingRequests.Count; ++i)
            {
                var request = m_LoadingRequests[i];
                if (request.Update())
                    continue;

                m_LoadingRequests.RemoveAt(i--);
            }

            if (m_UnusedRequests.Count > 0)
            {
                for (var i = 0; i < m_UnusedRequests.Count; ++i)
                {
                    var request = m_UnusedRequests[i];
                    if (!request.IsDone) continue;

                    INFO($"UnloadAsset: {request.AssetUrl}");
                    request.Unload();
                    m_UnusedRequests.RemoveAt(i--);
                }
            }

            bool unloadScene = false;
            for (var i = 0; i < m_SceneRequests.Count; ++i)
            {
                var request = m_SceneRequests[i];
                if (request.Update() || !request.IsUnused())
                    continue;
                INFO(string.Format("UnloadScene:{0}", request.AssetUrl));
                request.Unload();
                m_SceneRequests.RemoveAt(i--);

                unloadScene = true;
            }

            if (unloadScene)
            {
                if (Policy.UnloadAssetWhenUnloadScene)
                {
                    // 切场景时，也清理一次无用资源
                    UnloadUnusedAssets();
                }
            }
        }

        /// <summary>
        /// 加载资源
        /// <code>http 网络下载 只能异步，async = false 无效</code>
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <param name="async"></param>
        /// <returns></returns>
        private Request LoadAssetAsyncInternal(string path, Type type)
        {
            if (string.IsNullOrEmpty(path))
            {
                ERROR("null or empty path");
                return null;
            }

            if (m_RequestMap.TryGetValue(path, out Request request)) // 有缓存，直接取
            {
                request.Update();
                request.IncreaseRefCount();
                return request;
            }

            if (GetAssetBundleName(path, out string assetBundleName, out string subAssetPath)) // 没缓存取 ab 里拿
            {
                request = new AssetRequest(assetBundleName, subAssetPath);
            }
            else
            {
                // 需要直接加载remote的资源文件，自己去下载得了，这里就不管了
                //if (path.StartsWith("http://", StringComparison.Ordinal) ||
                //    path.StartsWith("https://", StringComparison.Ordinal) ||
                //    path.StartsWith("file://", StringComparison.Ordinal) ||
                //    path.StartsWith("ftp://", StringComparison.Ordinal) ||
                //    path.StartsWith("jar:file://", StringComparison.Ordinal))
                //{
                //    handle = new WebAssetHandle();
                //}
                //else
                {
                    // 都不是，则使用 AssetDatabase 加载
                    // 真机会报错
                    request = new AssetRequest_AssetDatabase();
                }
            }

            request.AssetUrl = path;
            request.AssetType = type;
            request.MoonAsset = this;

            m_RequestMap.Add(request.AssetUrl, request);
            m_LoadingRequests.Add(request);
            request.Load();
            request.IncreaseRefCount();

            __AnalyzeRequest(request);

            INFO($"{request.GetType().Name}: {request.AssetUrl}");

            return request;
        }

        #endregion

        #region Bundles

        /// <summary>
        /// 异步加载bundle，每帧加载数量限制
        /// <code>0 意为不限制</code>
        /// </summary>
        public int MaxBundlesPerFrame { get; set; } = 0;

        private readonly Dictionary<string, BundleRequest> m_BundleRequestMap = new(128, StringComparer.Ordinal); // 已加载bundle

        internal readonly List<BundleRequest> m_LoadingBundleRequests = new(64); // 正在加载bundle
        internal readonly Queue<BundleRequest> m_PendingBundleRequests = new(64); // 待加载bundle
        internal readonly List<BundleRequest> m_UnusedBundleRequests = new(128); // 引用为0，待卸载bundle

        public IReadOnlyDictionary<string, BundleRef> AssetToBundle => m_Manifest.AssetToBundle;
        public IReadOnlyDictionary<string, BundleRef[]> BundleToDeps => m_Manifest.BundleToDeps;
        public IReadOnlyDictionary<string, string> SpriteToAtlas => m_Manifest.SpriteToAtlas;

        private List<Manifest> m_AdditionalManifests = new();

        public void PatchManifest(Manifest manifest)
        {
            m_AdditionalManifests.Add(manifest);
        }

        public void RemoveManifest(Manifest manifest)
        {
            m_AdditionalManifests.Remove(manifest);
        }

        internal bool GetAssetBundleName(string assetPath, out string assetBundleName, out string subAssetPath)
        {
            subAssetPath = null;

#if UNITY_EDITOR
            if (s_Mode == EMode.AssetDatabase)
            {
                assetBundleName = null;
                return false;
            }
#endif

            // spriteatlas 需要中转一下
            if (TryGetSpriteToAtlas(assetPath, out var atlasPath))
            {
                subAssetPath = atlasPath;
                assetPath = atlasPath;
            }

            var ret = TryGetAssetToBundle(assetPath, out var bundleRef);
            if (ret)
            {
                assetBundleName = bundleRef.name;
            }
            else
            {
                assetBundleName = null;

                ERROR($"GetAssetBundleName failed. path: {assetPath}");
            }

            return ret;
        }

        internal bool TryGetSpriteToAtlas(string assetPath, out string atlasPath)
        {
            for (int i = m_AdditionalManifests.Count - 1; i >= 0; i--)
                if (m_AdditionalManifests[i].SpriteToAtlas.TryGetValue(assetPath, out atlasPath))
                    return true;

            return SpriteToAtlas.TryGetValue(assetPath, out atlasPath);
        }

        internal bool TryGetAssetToBundle(string assetPath, out BundleRef bundleRef)
        {
            for (int i = m_AdditionalManifests.Count - 1; i >= 0; i--)
                if (m_AdditionalManifests[i].AssetToBundle.TryGetValue(assetPath, out bundleRef))
                    return true;

            return AssetToBundle.TryGetValue(assetPath, out bundleRef);
        }

        internal BundleRef[] GetBundleDependencies(string assetBundleName)
        {
            BundleRef[] deps;

            for (int i = m_AdditionalManifests.Count - 1; i >= 0; i--)
                if (m_AdditionalManifests[i].BundleToDeps.TryGetValue(assetBundleName, out deps))
                    return deps;

            if (BundleToDeps.TryGetValue(assetBundleName, out deps))
                return deps;

            return null;
        }

        internal void UnloadBundle(BundleRequest bundle)
        {
            bundle.DecreaseRefCount();
        }

        private void UnloadDependencies(BundleRequest request)
        {
            for (var i = 0; i < request.Dependencies.Count; i++)
            {
                var depRequest = request.Dependencies[i];
                depRequest.DecreaseRefCount();
            }

            request.Dependencies.Clear();
        }

        private void LoadDependenciesAsync(BundleRequest request, string assetBundleName)
        {
            var dependencies = GetBundleDependencies(assetBundleName);
            if (dependencies == null || dependencies.Length <= 0)
                return;

            for (var i = 0; i < dependencies.Length; i++)
            {
                var depRef = dependencies[i];
                request.Dependencies.Add(LoadBundleAsync(depRef.name));
            }
        }

        internal BundleRequest LoadBundleAsync(string assetBundleName)
        {
            if (string.IsNullOrEmpty(assetBundleName))
            {
                ERROR("assetBundleName == null");
                return null;
            }

            bool exists = TryGetAssetPath(assetBundleName, out var bundlePath, out var remoteAssets);

            if (m_BundleRequestMap.TryGetValue(bundlePath, out var request))
            {
                request.Update();
                request.IncreaseRefCount();
                return request;
            }

            if (!exists)
            {
                if (remoteAssets != null) // 如果能获取到远端资源信息，就尝试去下载
                {
                    request = new BundleRequest_Remote
                    {
                        Info = new DownloadInfo
                        {
                            DownloadUrl = MoonAssetConfig.GetRemoteAssetURL(remoteAssets.Name),
                            SavePath = bundlePath,
                            Hash = remoteAssets.Hash,
                            Size = remoteAssets.Size,
                        },
                    };
                }
                else
                {
                    ERROR($"can't find remote asset: {bundlePath}");
                }
            }

            if (request == null)
            {
                if (FileUtility.IsHttpFile(bundlePath)) // http 使用 unitywebrequest 加载ab
                {
                    request = new BundleRequest_UWR();
                }
                else
                {
                    request = new BundleRequest_Local();
                }
            }

            request.AssetUrl = bundlePath;
            request.MoonAsset = this;

            m_BundleRequestMap.Add(bundlePath, request);

            if (MaxBundlesPerFrame > 0
                && m_LoadingRequests.Count >= MaxBundlesPerFrame
                && (request is BundleRequest_Local || request is BundleRequest_Remote))
            {
                // 当前异步加载的bundle超过 配置上限，加到待加载队列里去
                m_PendingBundleRequests.Enqueue(request);
            }
            else
            {
                request.Load();
                m_LoadingBundleRequests.Add(request);
                INFO($"{request.GetType().Name}: {request.AssetUrl}");
            }

            LoadDependenciesAsync(request, assetBundleName);

            request.IncreaseRefCount();
            __AnalyzeRequest(request);
            return request;
        }

        private void UpdateBundles(bool unloadAllObjects = true)
        {
            if (m_PendingBundleRequests.Count > 0 &&
                 MaxBundlesPerFrame > 0 &&
                 m_LoadingBundleRequests.Count < MaxBundlesPerFrame
                )
            {
                var toLoadCount = Math.Min(MaxBundlesPerFrame - m_LoadingBundleRequests.Count,
                    m_PendingBundleRequests.Count);
                while (toLoadCount > 0)
                {
                    var request = m_PendingBundleRequests.Dequeue();
                    if (request.LoadState == ELoadState.Init)
                    {
                        request.Load();
                        INFO("LoadBundle: " + request.AssetUrl);
                        m_LoadingBundleRequests.Add(request);
                    }

                    toLoadCount--;
                }
            }

            for (var i = 0; i < m_LoadingBundleRequests.Count; i++)
            {
                var request = m_LoadingBundleRequests[i];
                if (request.Update())
                    continue;
                m_LoadingBundleRequests.RemoveAt(i--);
            }

            // unload bundles
            if (m_UnusedBundleRequests.Count > 0)
            {
                for (var i = 0; i < m_UnusedBundleRequests.Count; i++)
                {
                    var request = m_UnusedBundleRequests[i];
                    if (!request.IsDone) continue;

                    UnloadDependencies(request);

                    request.Unload(unloadAllObjects);
                    INFO($"UnloadBundle: {request.AssetUrl} unloadAllObjects: {unloadAllObjects}");
                    m_UnusedBundleRequests.RemoveAt(i--);
                }
            }
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        // TODO show AssetHandleMap, BundleHandleMap. etc.
        [System.Diagnostics.DebuggerHidden]
        internal Dictionary<string, List<IAssetHandle>> AnalyzeHandles { get; private set; } = new(1024);
#endif

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void __AnalyzeRequest(IAssetHandle handle)
        {
#if UNITY_EDITOR
            if (!AnalyzeHandles.TryGetValue(handle.AssetUrl, out var list))
            {
                list = new List<IAssetHandle>();
                AnalyzeHandles.Add(handle.AssetUrl, list);
            }

            list.Add(handle);
#endif
        }

        [System.Diagnostics.Conditional("DEBUG_MOONASSET")]
        internal static void INFO(string msg) => Log.INFO("MoonAsset", msg);

        [System.Diagnostics.Conditional("DEBUG_MOONASSET")]
        internal static void WARN(string msg) => Log.WARN("MoonAsset", msg);

        internal static void ERROR(string msg) => Log.ERROR("MoonAsset", msg);

        #endregion
    }
}