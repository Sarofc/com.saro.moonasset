using UnityEngine;

namespace Saro.XAsset
{
    public class BundleAssetHandle : AssetHandle
    {
        protected readonly string m_AssetBundleName;
        protected BundleHandle m_BundleHandle;

        public BundleAssetHandle(string bundle)
        {
            m_AssetBundleName = bundle;
        }

        internal override void Load()
        {
            // fix
            // 同一帧，先调异步接口，再调用同步接口，同步接口 bundle.assetBundle 报空
            // （不确定异步加载bundle未完成时的情况）

            m_BundleHandle = XAssetManager.Current.LoadBundle(m_AssetBundleName);

            if (typeof(Component).IsAssignableFrom(AssetType))
            {
                var gameObject = m_BundleHandle.Bundle.LoadAsset<GameObject>(AssetUrl);
                Asset = gameObject.GetComponent(AssetType);
            }
            else
            {
                Asset = m_BundleHandle.Bundle.LoadAsset(AssetUrl, AssetType);
            }
        }

        internal override void Unload(bool unloadAllObjects = true)
        {
            if (m_BundleHandle != null)
            {
                m_BundleHandle.DecreaseRefCount();
                m_BundleHandle = null;
            }

            // 这里依赖 Bundle.Unload(true) 来卸载资源
            Asset = null;
        }
    }
}