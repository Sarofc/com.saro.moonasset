using UnityEngine;

namespace Saro.MoonAsset
{
    public class BundleAssetHandle : AssetHandle
    {
        protected readonly string m_AssetBundleName;
        protected BundleHandle bundleHandle;

        public BundleAssetHandle(string bundle)
        {
            m_AssetBundleName = bundle;
        }

        internal override void Load()
        {
            // fix
            // 同一帧，先调异步接口，再调用同步接口，同步接口 bundle.assetBundle 报空
            // （不确定异步加载bundle未完成时的情况）

            bundleHandle = MoonAsset.Current.LoadBundle(m_AssetBundleName);

            if (typeof(Component).IsAssignableFrom(AssetType))
            {
                var gameObject = bundleHandle.Bundle.LoadAsset<GameObject>(AssetUrl);
                Asset = gameObject.GetComponent(AssetType);
            }
            else
            {
                Asset = bundleHandle.Bundle.LoadAsset(AssetUrl, AssetType);
            }
        }

        internal override void Unload(bool unloadAllObjects = true)
        {
            if (bundleHandle != null)
            {
                bundleHandle.DecreaseRefCount();
                bundleHandle = null;
            }

            // 这里依赖 Bundle.Unload(true) 来卸载资源
            Asset = null;
        }
    }
}