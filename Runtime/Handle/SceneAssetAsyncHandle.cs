using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Saro.MoonAsset
{
    public class SceneAssetAsyncHandle : SceneAssetHandle
    {
        private AsyncOperation m_AsyncOperation;

        public SceneAssetAsyncHandle(string path, bool additive)
            : base(path, additive)
        {
        }

        public override float Progress
        {
            get
            {
                if (handle == null)
                    return m_AsyncOperation == null ? 0 : m_AsyncOperation.progress;

                var bundleProgress = handle.Progress;
                if (handle.Dependencies.Count <= 0)
                    return bundleProgress * 0.3f + (m_AsyncOperation != null ? m_AsyncOperation.progress * 0.7f : 0);
                for (int i = 0, max = handle.Dependencies.Count; i < max; i++)
                {
                    var item = handle.Dependencies[i];
                    bundleProgress += item.Progress;
                }

                return bundleProgress / (handle.Dependencies.Count + 1) * 0.3f +
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
                            if (handle == null || handle.Error != null)
                                return true;

                            for (int i = 0, max = handle.Dependencies.Count; i < max; i++)
                            {
                                var item = handle.Dependencies[i];
                                if (item.Error != null)
                                    return true;
                            }

                            if (!handle.IsDone)
                                return false;

                            for (int i = 0, max = handle.Dependencies.Count; i < max; i++)
                            {
                                var item = handle.Dependencies[i];
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
                Debug.LogException(e);
                Error = e.ToString();
                LoadState = ELoadState.Loaded;
            }
        }

        internal override void Load()
        {
            if (!string.IsNullOrEmpty(m_AssetBundleName))
            {
                handle = MoonAsset.Current.LoadBundleAsync(m_AssetBundleName);
                LoadState = ELoadState.LoadAssetBundle;
            }
            else
            {
                LoadSceneAsync();
            }
        }

        internal override void Unload(bool unloadAllObjects = true)
        {
            base.Unload(unloadAllObjects);
            m_AsyncOperation = null;
        }
    }
}