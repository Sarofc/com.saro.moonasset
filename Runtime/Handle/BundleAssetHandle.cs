using System;
using System.IO;
using UnityEngine;
using UnityEngine.U2D;

namespace Saro.MoonAsset
{
    public class BundleAssetHandle : AssetHandle
    {
        protected readonly string m_AssetBundleName;
        protected BundleHandle m_BundleHandle;

        /// <summary>
        /// 间接寻址用，sprite(AssetsUrl) -> spriteatlas(SubAssetUrl) -> assetbundle
        /// </summary>
        protected string m_SubAssetUrl;

        public bool IsComponent => typeof(Component).IsAssignableFrom(AssetType);

        public bool IsSpriteAtlas => m_SubAssetUrl != null && (m_SubAssetUrl.EndsWith(".spriteatlas", StringComparison.Ordinal) || m_SubAssetUrl.EndsWith(".spriteatlasv2", StringComparison.Ordinal));

        public BundleAssetHandle(string bundle, string subAsserUrl)
        {
            m_AssetBundleName = bundle;
            m_SubAssetUrl = subAsserUrl;
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
                var atlas = m_BundleHandle.Bundle.LoadAsset<SpriteAtlas>(m_SubAssetUrl);
                Asset = atlas.GetSprite(Path.GetFileNameWithoutExtension(AssetUrl));
            }
            else
            {
                Asset = m_BundleHandle.Bundle.LoadAsset(AssetUrl, AssetType);
            }

            if (Asset == null)
                Error = $"load asset failed. url: {AssetUrl} type: {AssetType}";
        }

        internal override void Unload(bool unloadAllObjects = true)
        {
            if (m_BundleHandle != null)
            {
                m_BundleHandle.DecreaseRefCount();
                m_BundleHandle = null;
            }

            // 这里依赖 Bundle.Unload(true) 来真正卸载资源
            Asset = null;
        }
    }
}