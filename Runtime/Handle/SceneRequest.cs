using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Saro.MoonAsset
{
    public class SceneRequest : Request
    {
        private AsyncOperation m_AsyncOperation;

        protected readonly LoadSceneMode m_LoadSceneMode;
        protected readonly string m_SceneName;
        protected string m_AssetBundleName;
        protected BundleRequest m_BundleRequest;

        public SceneRequest(string path, bool additive)
        {
            AssetUrl = path;
            MoonAsset.Current.GetAssetBundleName(path, out m_AssetBundleName, out _);
            m_SceneName = Path.GetFileNameWithoutExtension(AssetUrl);
            m_LoadSceneMode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single;
        }

        public override float Progress
        {
            get
            {
                if (m_BundleRequest == null)
                    return m_AsyncOperation == null ? 0 : m_AsyncOperation.progress;

                var bundleProgress = m_BundleRequest.Progress;
                if (m_BundleRequest.Dependencies.Count <= 0)
                    return bundleProgress * 0.3f + (m_AsyncOperation != null ? m_AsyncOperation.progress * 0.7f : 0);
                for (int i = 0, max = m_BundleRequest.Dependencies.Count; i < max; i++)
                {
                    var item = m_BundleRequest.Dependencies[i];
                    bundleProgress += item.Progress;
                }

                return bundleProgress / (m_BundleRequest.Dependencies.Count + 1) * 0.3f +
                       (m_AsyncOperation != null ? m_AsyncOperation.progress * 0.7f : 0);
            }
        }

        public override bool IsDone
        {
            get
            {
                switch (LoadState)
                {
                    case ELoadState.Loaded:
                        return true;
                    case ELoadState.LoadAssetBundle:
                        {
                            if (m_BundleRequest == null || m_BundleRequest.IsError)
                            {
                                Error = $"[{nameof(SceneRequest)}] loadscene failed. url: {m_BundleRequest.AssetUrl} error: {m_BundleRequest.Error}";
                                return true;
                            }

                            for (int i = 0, max = m_BundleRequest.Dependencies.Count; i < max; i++)
                            {
                                var item = m_BundleRequest.Dependencies[i];
                                if (item == null || item.IsError)
                                {
                                    Error = $"[{nameof(SceneRequest)}] load dependencies failed. url: {item.AssetUrl} error: {item.Error}";
                                    return true;
                                }
                            }

                            if (!m_BundleRequest.IsDone)
                                return false;

                            for (int i = 0, max = m_BundleRequest.Dependencies.Count; i < max; i++)
                            {
                                var item = m_BundleRequest.Dependencies[i];
                                if (!item.IsDone)
                                    return false;
                            }

                            LoadSceneAsync();

                            break;
                        }
                    case ELoadState.Unload:
                        break;
                    case ELoadState.LoadAsset:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (LoadState != ELoadState.LoadAsset)
                    return false;
                if (m_AsyncOperation != null && !m_AsyncOperation.isDone)
                    return false;

                LoadState = ELoadState.Loaded;
                return true;
            }
        }

        private void LoadSceneAsync()
        {
            try
            {
                m_AsyncOperation = SceneManager.LoadSceneAsync(m_SceneName, m_LoadSceneMode);
                LoadState = ELoadState.LoadAsset;
            }
            catch (Exception e)
            {
                Error = e.ToString();
                LoadState = ELoadState.Loaded;
            }
        }

        internal override void Load()
        {
            if (!string.IsNullOrEmpty(m_AssetBundleName))
            {
                m_BundleRequest = MoonAsset.Current.LoadBundleAsync(m_AssetBundleName);
                LoadState = ELoadState.LoadAssetBundle;
            }
            else // editoronly
            {
                LoadSceneAsync();
            }
        }

        internal override void Unload(bool unloadAllObjects = true)
        {
            if (m_BundleRequest != null)
                m_BundleRequest.DecreaseRefCount();

            if (m_LoadSceneMode == LoadSceneMode.Additive)
            {
                if (SceneManager.GetSceneByName(m_SceneName).isLoaded)
                    SceneManager.UnloadSceneAsync(m_SceneName);
            }

            m_BundleRequest = null;

            m_AsyncOperation = null;
        }

        public override void WaitForCompletion()
        {
            throw new NotSupportedException($"[MoonAsset] scene only support async load");
        }
    }
}