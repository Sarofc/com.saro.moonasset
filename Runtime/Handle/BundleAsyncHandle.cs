using UnityEngine;

namespace Saro.MoonAsset
{
    public class BundleAsyncHandle : BundleHandle
    {
        private AssetBundleCreateRequest m_Request;

        public override AssetBundle Bundle
        {
            get
            {
                // fix 
                // 同一帧，先调异步接口，再调用同步接口，同步接口 bundle.assetBundle 报空
                if (m_Request != null && !m_Request.isDone)
                {
                    Asset = m_Request.assetBundle;
                    //Debug.LogError("bundle async request is not done. asset = " + (asset ? asset.name : "null"));
                }
                return base.Bundle;
            }
            internal set { base.Bundle = value; }
        }

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
                    Asset = m_Request.assetBundle;
                    if (Asset == null)
                    {
                        Error = string.Format("unable to load assetBundle:{0}", AssetUrl);
                    }

                    LoadState = ELoadState.Loaded;
                }

                return m_Request == null || m_Request.isDone;
            }
        }

        public override float Progress => m_Request != null ? m_Request.progress : 0f;

        internal override void Load()
        {
            m_Request = AssetBundle.LoadFromFileAsync(AssetUrl);

            if (m_Request == null)
            {
                Error = AssetUrl + " LoadFromFile failed.";
                LoadState = ELoadState.Loaded;
                return;
            }

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