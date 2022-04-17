using System.Collections.Generic;

namespace Saro.XAsset
{
    public partial class XAssetManager
    {
        /// <summary>
        /// 资源路径定位器
        /// </summary>
        /// <param name="assetName">资源相对路径</param>
        /// <param name="assetPath">资源完整路径</param>
        /// <param name="remoteAssets">远端资源对象</param>
        /// <returns>false代表本地没有找到资源</returns>
        public delegate bool AssetLocator(string assetName, ref string assetPath, ref IRemoteAssets remoteAssets);

        private List<AssetLocator> m_AssetLocators = new List<AssetLocator>();

        public void AddAssetLocator(AssetLocator locator)
        {
            m_AssetLocators.Add(locator);
        }

        /// <summary>
        /// 获取Manifest管理的资源路径
        /// </summary>
        /// <param name="assetName">相对路径资源名 eg. Bundles/xxx.assets</param>
        /// <param name="assetPath">资源物理地址，绝对路径</param>
        /// <param name="remoteAssets">远端资源信息</param>
        /// <returns>本地是否存在</returns>
        public bool TryGetAssetPath(string assetName, out string assetPath, out IRemoteAssets remoteAssets)
        {
            assetPath = null;
            remoteAssets = null;

            // bundle 编辑器模式下 根本不会 进来

            for (int i = 0; i < m_AssetLocators.Count; i++)
            {
                var locator = m_AssetLocators[i];
                bool result = locator(assetName, ref assetPath, ref remoteAssets);

                if (result)
                    return true;
            }

            return false;
        }
    }
}
