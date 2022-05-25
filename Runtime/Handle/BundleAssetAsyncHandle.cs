using System;
using UnityEngine;

namespace Saro.MoonAsset
{
    public class BundleAssetAsyncHandle : BundleAssetHandle
    {
        private AssetBundleRequest m_AssetBundleRequest;

        public BundleAssetAsyncHandle(string bundle)
            : base(bundle)
        {
        }

        public override bool IsDone
        {
            get
            {
                switch (LoadState)
                {
                    case ELoadState.Init:
                        return false;
                    case ELoadState.Loaded:
                        return true;
                    case ELoadState.LoadAssetBundle:
                        {
                            if (Error != null || bundleHandle.Error != null)
                                return true;

                            for (int i = 0, max = bundleHandle.Dependencies.Count; i < max; i++)
                            {
                                var item = bundleHandle.Dependencies[i];
                                if (item.Error != null)
                                    return true;
                            }

                            if (!bundleHandle.IsDone)
                                return false;

                            for (int i = 0, max = bundleHandle.Dependencies.Count; i < max; i++)
                            {
                                var item = bundleHandle.Dependencies[i];
                                if (!item.IsDone)
                                    return false;
                            }

                            if (bundleHandle.Bundle == null)
                            {
                                Error = "assetBundle == null";
                                return true;
                            }

                            if (typeof(Component).IsAssignableFrom(AssetType))
                            {
                                m_AssetBundleRequest = bundleHandle.Bundle.LoadAssetAsync(AssetUrl, typeof(GameObject));
                            }
                            else
                            {
                                m_AssetBundleRequest = bundleHandle.Bundle.LoadAssetAsync(AssetUrl, AssetType);
                            }

                            LoadState = ELoadState.LoadAsset;
                            break;
                        }
                    case ELoadState.Unload:
                        return false;
                    case ELoadState.LoadAsset:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (!m_AssetBundleRequest.isDone)
                    return false;

                if (typeof(Component).IsAssignableFrom(AssetType))
                {
                    var gameObject = m_AssetBundleRequest.asset as GameObject;
                    Asset = gameObject.GetComponent(AssetType);
                }
                else
                {
                    Asset = m_AssetBundleRequest.asset;
                }

                LoadState = ELoadState.Loaded;
                return true;
            }
        }

        public override float Progress
        {
            get
            {
                var bundleProgress = bundleHandle.Progress;
                if (bundleHandle.Dependencies.Count <= 0)
                    return bundleProgress * 0.3f + (m_AssetBundleRequest != null ? m_AssetBundleRequest.progress * 0.7f : 0);
                for (int i = 0, max = bundleHandle.Dependencies.Count; i < max; i++)
                {
                    var item = bundleHandle.Dependencies[i];
                    bundleProgress += item.Progress;
                }

                return bundleProgress / (bundleHandle.Dependencies.Count + 1) * 0.3f +
                       (m_AssetBundleRequest != null ? m_AssetBundleRequest.progress * 0.7f : 0);
            }
        }

        internal override void Load()
        {
            bundleHandle = MoonAsset.Current.LoadBundleAsync(m_AssetBundleName);
            LoadState = ELoadState.LoadAssetBundle;
        }

        internal override void Unload(bool unloadAllObjects = true)
        {
            base.Unload(unloadAllObjects);
            m_AssetBundleRequest = null;
            LoadState = ELoadState.Unload;
        }
    }
}