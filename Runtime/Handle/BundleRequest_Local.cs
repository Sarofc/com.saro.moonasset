using System.Runtime.InteropServices;
using UnityEngine;

namespace Saro.MoonAsset
{
    public class BundleRequest_Local : BundleRequest
    {
        private AssetBundleCreateRequest m_AssetBundleCreateRequest;

        private bool m_IsWaitForCompletion;

        public override bool IsDone
        {
            get
            {
                if (LoadState == ELoadState.Init)
                    return false;

                if (LoadState == ELoadState.Loaded)
                    return true;

                if (LoadState == ELoadState.LoadAssetBundle)
                {
                    if (m_IsWaitForCompletion)
                    {
                        Asset = m_AssetBundleCreateRequest.assetBundle; // 强制同步加载
                        MoonAsset.WARN($"[{nameof(BundleRequest_Local)}] sync load: {Asset}");
                        LoadState = ELoadState.Loaded;
                    }
                    else
                    {
                        if (m_AssetBundleCreateRequest.isDone)
                        {
                            Asset = m_AssetBundleCreateRequest.assetBundle;
                            LoadState = ELoadState.Loaded;
                        }
                    }
                }

                if (LoadState == ELoadState.Loaded)
                {
                    if (Bundle == null)
                        Error = $"[{nameof(BundleRequest_Local)}] load assetBundle failed. url: {AssetUrl}";

                    return true;
                }

                return false;
            }
        }

        public override float Progress => m_AssetBundleCreateRequest != null ? m_AssetBundleCreateRequest.progress : 0f;

        internal override void Load()
        {
            m_AssetBundleCreateRequest = AssetBundle.LoadFromFileAsync(AssetUrl);
            LoadState = ELoadState.LoadAssetBundle;
        }

        internal override void Unload(bool unloadAllObjects = true)
        {
            m_AssetBundleCreateRequest = null;
            LoadState = ELoadState.Unload;

            if (Bundle == null)
                return;
            Bundle.Unload(unloadAllObjects);
            Bundle = null;

            m_IsWaitForCompletion = false;
        }

        public override void WaitForCompletion()
        {
            m_IsWaitForCompletion = true;

            var span = CollectionsMarshal.AsSpan(Dependencies);
            foreach (var dep in span)
                dep.WaitForCompletion();

            InvokeWaitForCompletion();

#if DEBUG
            if (!IsDone)
                MoonAsset.ERROR("WaitForCompletion fata error");
#endif
        }

        //internal override bool InvokeWaitForCompletion()
        //{
        //    int frame = 1000;

        //    while (Update())
        //    {
        //        if (--frame == 0)
        //        {
        //            MoonAsset.ERROR($"sync load timeout. {AssetUrl}");
        //            break;
        //        }
        //    }

        //    return true;
        //}
    }
}