using System;
using System.IO;
using UnityEngine;
using UnityEngine.U2D;

using UObject = UnityEngine.Object;

namespace Saro.MoonAsset
{
    public class AssetRequest : Request
    {
        protected readonly string m_AssetBundleName;
        protected BundleRequest m_BundleRequest;

        /// <summary>
        /// 间接寻址用，sprite(AssetsUrl) -> spriteatlas(SubAssetUrl) -> assetbundle
        /// </summary>
        protected string m_SubAssetUrl;

        public bool IsComponent => typeof(Component).IsAssignableFrom(AssetType);

        public bool IsSpriteAtlas => m_SubAssetUrl != null && (m_SubAssetUrl.EndsWith(".spriteatlas", StringComparison.Ordinal) || m_SubAssetUrl.EndsWith(".spriteatlasv2", StringComparison.Ordinal));

        private AssetBundleRequest m_AssetBundleRequest;

        private bool m_IsWaitForCompletion;

        public AssetRequest(string bundle, string subAsserUrl)
        {
            m_AssetBundleName = bundle;
            m_SubAssetUrl = subAsserUrl;
        }

        public override bool IsDone
        {
            get
            {
                if (LoadState == ELoadState.Init) return false;
                if (LoadState == ELoadState.Loaded) return true;
                if (LoadState == ELoadState.LoadAssetBundle)
                {
                    if (Error != null || m_BundleRequest.Error != null)
                        return true;

                    for (int i = 0, max = m_BundleRequest.Dependencies.Count; i < max; i++)
                    {
                        var item = m_BundleRequest.Dependencies[i];
                        if (item.Error != null)
                            return true;
                    }

                    if (!m_BundleRequest.IsDone)
                        return false;

                    for (int i = 0, max = m_BundleRequest.Dependencies.Count; i < max; i++)
                    {
                        var item = m_BundleRequest.Dependencies[i];
                        if (!item.IsDone)
                            return false;
                    }

                    if (m_BundleRequest.Bundle == null)
                        return true;

                    if (IsSpriteAtlas)
                    {
                        m_AssetBundleRequest = m_BundleRequest.Bundle.LoadAssetAsync(m_SubAssetUrl, typeof(SpriteAtlas));
                    }
                    else
                    {
                        m_AssetBundleRequest = m_BundleRequest.Bundle.LoadAssetAsync(AssetUrl, AssetType);
                    }

                    LoadState = ELoadState.LoadAsset;
                }

                if (LoadState == ELoadState.LoadAsset)
                {
                    if (!m_IsWaitForCompletion && !m_AssetBundleRequest.isDone)
                        return false;

                    if (IsComponent)
                    {
                        MoonAsset.ERROR($"not support typeof(Component) {AssetType}, use GameObject instead. url: {Path.GetFileName(AssetUrl)}");

                        //var gameObject = m_AssetBundleRequest.asset as GameObject;
                        //Asset = gameObject.GetComponent(AssetType);
                    }
                    else if (IsSpriteAtlas)
                    {
                        var atlas = m_AssetBundleRequest.asset as SpriteAtlas;
                        Asset = atlas.GetSprite(Path.GetFileNameWithoutExtension(AssetUrl));
                    }
                    else
                    {
                        Asset = m_AssetBundleRequest.asset;
                    }

                    if (Asset == null)
                        Error = $"load asset failed. url: {AssetUrl} type: {AssetType}";

                    LoadState = ELoadState.Loaded;
                    return true;
                }

                return false;
            }
        }

        public override float Progress
        {
            get
            {
                var bundleProgress = m_BundleRequest.Progress;
                if (m_BundleRequest.Dependencies.Count <= 0)
                    return bundleProgress * 0.3f + (m_AssetBundleRequest != null ? m_AssetBundleRequest.progress * 0.7f : 0);
                for (int i = 0, max = m_BundleRequest.Dependencies.Count; i < max; i++)
                {
                    var item = m_BundleRequest.Dependencies[i];
                    bundleProgress += item.Progress;
                }

                return bundleProgress / (m_BundleRequest.Dependencies.Count + 1) * 0.3f +
                       (m_AssetBundleRequest != null ? m_AssetBundleRequest.progress * 0.7f : 0);
            }
        }

        internal override void Load()
        {
            m_BundleRequest = MoonAsset.Current.LoadBundleAsync(m_AssetBundleName);
            LoadState = ELoadState.LoadAssetBundle;
        }

        internal override void Unload(bool unloadAllObjects = true)
        {
            m_AssetBundleRequest = null;
            LoadState = ELoadState.Unload;

            if (m_BundleRequest != null)
            {
                m_BundleRequest.DecreaseRefCount();
                m_BundleRequest = null;
            }

            // 这里依赖 Bundle.Unload(true) 来真正卸载资源
            Asset = null;

            m_IsWaitForCompletion = false;
        }

        public override void WaitForCompletion()
        {
            m_IsWaitForCompletion = true;

            m_BundleRequest.WaitForCompletion();

            InvokeWaitForCompletion();

#if DEBUG
            if (!IsDone)
                MoonAsset.ERROR("WaitForCompletion fata error");
#endif
        }
    }
}