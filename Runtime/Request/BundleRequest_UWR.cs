using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Saro.MoonAsset
{
    /// <summary>
    /// TODO 这个也要测试下
    /// 
    /// 直接加载远端的assetbundle，使用UnityWebRequestAssetBundle，针对webgl
    /// </summary>
    public class BundleRequest_UWR : BundleRequest
    {
        private UnityWebRequest m_UnityWebRequest;

        public override bool IsDone
        {
            get
            {
                if (LoadState == ELoadState.Init)
                    return false;

                if (LoadState == ELoadState.Loaded)
                    return true;

                if (LoadState == ELoadState.LoadAssetBundle && m_UnityWebRequest.isDone)
                {
                    Asset = DownloadHandlerAssetBundle.GetContent(m_UnityWebRequest);

                    if (Bundle == null)
                        Error = $"load assetBundle failed. url: {AssetUrl}";

                    LoadState = ELoadState.Loaded;
                }

                return m_UnityWebRequest == null || m_UnityWebRequest.isDone;
            }
        }

        public override float Progress => m_UnityWebRequest != null ? m_UnityWebRequest.downloadProgress : 0f;

        internal override void Load()
        {
            m_UnityWebRequest = UnityWebRequestAssetBundle.GetAssetBundle(AssetUrl);
            m_UnityWebRequest.SendWebRequest();

            LoadState = ELoadState.LoadAssetBundle;
        }

        internal override void Unload(bool unloadAllObjects = true)
        {
            m_UnityWebRequest = null;
            LoadState = ELoadState.Unload;

            if (Bundle == null)
                return;
            Bundle.Unload(unloadAllObjects);
            Bundle = null;
        }

        public override void WaitForCompletion()
        {
            InvokeWaitForCompletion();

#if DEBUG
            if (!IsDone)
                MoonAsset.ERROR("WaitForCompletion fatal error");
#endif
        }
    }
}