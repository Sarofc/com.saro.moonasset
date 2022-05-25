using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Saro.MoonAsset
{
    public class SceneAssetHandle : AssetHandle
    {
        protected readonly LoadSceneMode m_LoadSceneMode;
        protected readonly string m_SceneName;
        protected string m_AssetBundleName;
        protected BundleHandle handle;

        public SceneAssetHandle(string path, bool additive)
        {
            AssetUrl = path;
            MoonAsset.Current.GetAssetBundleName(path, out m_AssetBundleName);
            m_SceneName = Path.GetFileNameWithoutExtension(AssetUrl);
            m_LoadSceneMode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single;
        }

        public override float Progress
        {
            get { return 1; }
        }

        internal override void Load()
        {
            if (!string.IsNullOrEmpty(m_AssetBundleName))
            {
                handle = MoonAsset.Current.LoadBundle(m_AssetBundleName);
                if (handle != null)
                    SceneManager.LoadScene(m_SceneName, m_LoadSceneMode);
            }
            else
            {
                try
                {
                    SceneManager.LoadScene(m_SceneName, m_LoadSceneMode);
                    LoadState = ELoadState.LoadAsset;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Error = e.ToString();
                    LoadState = ELoadState.Loaded;
                }
            }
        }

        internal override void Unload(bool unloadAllObjects = true)
        {
            if (handle != null)
                handle.DecreaseRefCount();

            if (m_LoadSceneMode == LoadSceneMode.Additive)
            {
                if (SceneManager.GetSceneByName(m_SceneName).isLoaded)
                    SceneManager.UnloadSceneAsync(m_SceneName);
            }

            handle = null;
        }
    }
}