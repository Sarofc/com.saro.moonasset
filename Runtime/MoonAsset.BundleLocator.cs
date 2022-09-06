using System.Collections.Generic;
using Saro.Core;

namespace Saro.MoonAsset
{
    public partial class MoonAsset
    {
        /// <summary>
        /// 设置 MoonAsset 默认 AssetLocator 加载列表
        /// </summary>
        public void SetDefaultLocators()
        {
            AddBundleLocators(GetDefaultLocators());
        }

        /// <summary>
        /// MoonAsset 默认 AssetLocator 加载列表
        /// </summary>
        public static IList<BundleLocator> GetDefaultLocators()
        {
            var locators = new List<BundleLocator>();

            // 1. 编辑器模式直接加载源文件
            // 2. 模拟模式，优先加载打包目录
#if UNITY_EDITOR
            if (s_Mode == EMode.Simulate)
            {
                locators.Add(new LocalBundleLocator(MoonAssetConfig.k_Editor_DlcOutputPath));
            }
#endif

            // 1.先加载dlc目录
            locators.Add(new LocalBundleLocator(MoonAssetConfig.k_DlcPath));
            // 2.再加载base目录
            locators.Add(new LocalBundleLocator(MoonAssetConfig.k_BasePath));
            // 3.都没有就下载到dlc目录，再加载
            locators.Add(new RemoteBundleLocator(MoonAssetConfig.k_DlcPath));

            return locators;
        }

        /// <summary>
        /// 资源路径定位器
        /// </summary>
        /// <param name="assetName">资源相对路径</param>
        /// <param name="assetPath">资源完整路径</param>
        /// <param name="remoteAssets">远端资源对象</param>
        /// <returns>false代表本地没有找到资源</returns>
        public delegate bool BundleLocator(string assetName, ref string assetPath, ref IRemoteAssets remoteAssets);

        private readonly List<BundleLocator> m_AssetLocators = new();

        public void AddBundleLocators(IList<BundleLocator> locators)
        {
            foreach (var locator in locators)
            {
                AddBundleLocator(locator);
            }
        }

        public void AddBundleLocator(BundleLocator locator)
        {
            m_AssetLocators.Add(locator);
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

            // bundle 编辑器模式下 根本不会 进来

            if (m_AssetLocators == null || m_AssetLocators.Count == 0)
            {
                ERROR($"AddAssetLocator first");
                return false;
            }

            for (int i = 0; i < m_AssetLocators.Count; i++)
            {
                var locator = m_AssetLocators[i];
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