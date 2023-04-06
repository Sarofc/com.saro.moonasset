using System.Collections.Generic;

namespace Saro.MoonAsset
{
    public partial class MoonAsset
    {
        [System.Obsolete("Use 'AddDefaultLocators' instead")]
        public void SetDefaultLocators()
        {
            AddDefaultLocators();
        }

        /// <summary>
        /// 设置 MoonAsset 默认 AssetLocator 加载列表
        /// </summary>
        public void AddDefaultLocators()
        {
            // 4.都没有就下载到dlc目录，再加载
            AddBundleLocator(new RemoteBundleLocator(MoonAssetConfig.k_DlcPath));

            // 3.再加载base目录
            AddBundleLocator(new LocalBundleLocator(MoonAssetConfig.k_BasePath));

            // 2.先加载dlc目录
            AddBundleLocator(new LocalBundleLocator(MoonAssetConfig.k_DlcPath));

#if UNITY_EDITOR
            // 1. 模拟模式，优先加载打包目录
            if (s_Mode == EMode.Simulate)
            {
                AddBundleLocator(new LocalBundleLocator(MoonAssetConfig.k_Editor_DlcOutputPath));
            }

            // 0. 编辑器模式，使用AssetDataBase，不使用bundle
#endif
        }

        /// <summary>
        /// Bundle路径定位器
        /// </summary>
        /// <param name="assetName">资源相对路径</param>
        /// <param name="assetPath">资源完整路径</param>
        /// <param name="remoteAssets">远端资源对象</param>
        /// <returns>false代表本地没有找到资源</returns>
        public delegate bool BundleLocator(string assetName, ref string assetPath, ref IRemoteAssets remoteAssets);

        private readonly List<BundleLocator> m_BundleLocators = new();

        /// <summary>
        /// 添加Bundle路径定位器
        /// <code>WARN: 后添加的，先调用</code>
        /// </summary>
        /// <param name="locator"></param>
        public void AddBundleLocator(BundleLocator locator)
        {
            m_BundleLocators.Add(locator);
        }

        /// <summary>
        /// 获取Manifest管理的资源路径
        /// </summary>
        /// <param name="bundleName">相对路径资源名 eg. Bundles/xxx.assets</param>
        /// <param name="filePath">资源物理地址，绝对路径</param>
        /// <param name="remoteAssets">远端资源信息</param>
        /// <returns>本地是否存在</returns>
        public bool TryGetAssetPath(string bundleName, out string filePath, out IRemoteAssets remoteAssets)
        {
            filePath = null;
            remoteAssets = null;

            // bundle 编辑器模式下 不会进来这里

            if (m_BundleLocators == null || m_BundleLocators.Count == 0)
            {
                ERROR($"AddAssetLocator first");
                return false;
            }

            // 后添加的，先调用
            for (int i = m_BundleLocators.Count - 1; i >= 0; i--)
            {
                var locator = m_BundleLocators[i];
                bool result = locator(bundleName, ref filePath, ref remoteAssets);

                if (result)
                    return true;
            }

            if (remoteAssets == null)
                ERROR($"TryGetAssetPath failed. AssetName: {bundleName}");

            return false;
        }
    }
}