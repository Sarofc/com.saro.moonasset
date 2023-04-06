using UnityEngine;
using UnityEngine.Networking;

namespace Saro.MoonAsset
{
    /// <summary>
    /// 直接加载远端的assetbundle，针对webgl
    /// </summary>
    public class RemoteBundleAsyncHandle : BundleHandle
    {
        private UnityWebRequest m_Request;

        public override bool IsDone
        {
            get
            {
                if (LoadState == ELoadState.Init)
                    return false;

                if (LoadState == ELoadState.Loaded)
                    return true;

                if (LoadState == ELoadState.LoadAssetBundle && m_Request.isDone)
                {
                    Asset = DownloadHandlerAssetBundle.GetContent(m_Request);

                    if (Asset == null)
                        Error = $"load assetBundle failed. url: {AssetUrl}";

                    LoadState = ELoadState.Loaded;
                }

                return m_Request == null || m_Request.isDone;
            }
        }

        public override float Progress => m_Request != null ? m_Request.downloadProgress : 0f;

        internal override void Load()
        {
            m_Request = UnityWebRequestAssetBundle.GetAssetBundle(AssetUrl);
            m_Request.SendWebRequest();

            LoadState = ELoadState.LoadAssetBundle;
        }

        internal override void Unload(bool unloadAllObjects = true)
        {
            m_Request = null;
            LoadState = ELoadState.Unload;
            base.Unload(unloadAllObjects);
        }
    }
}