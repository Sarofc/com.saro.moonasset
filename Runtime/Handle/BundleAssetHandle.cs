using System.IO;
using UnityEngine;
using UnityEngine.U2D;

namespace Saro.MoonAsset
{
    public class BundleAssetHandle : AssetHandle
    {
        protected readonly string m_AssetBundleName;
        protected BundleHandle m_BundleHandle;

        public bool IsComponent => typeof(Component).IsAssignableFrom(AssetType);

        public bool IsSpriteAtlas => SubAssetUrl != null && (SubAssetUrl.EndsWith(".spriteatlas") || SubAssetUrl.EndsWith(".spriteatlasv2"));

        public BundleAssetHandle(string bundle)
        {
            m_AssetBundleName = bundle;
        }

        internal override void Load()
        {
            // fix
            // 同一帧，先调异步接口，再调用同步接口，同步接口 bundle.assetBundle 报空
            // （不确定异步加载bundle未完成时的情况）

            m_BundleHandle = MoonAsset.Current.LoadBundle(m_AssetBundleName);

            if (IsComponent)
            {
                var gameObject = m_BundleHandle.Bundle.LoadAsset<GameObject>(AssetUrl);
                Asset = gameObject.GetComponent(AssetType);
            }
            else if (IsSpriteAtlas)
            {
                var atlas = m_BundleHandle.Bundle.LoadAsset<SpriteAtlas>(SubAssetUrl);
                Asset = atlas.GetSprite(Path.GetFileNameWithoutExtension(AssetUrl));
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