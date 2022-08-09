using System.Collections.Generic;

namespace Saro.MoonAsset
{
    public partial class MoonAsset
    {
        public MoonAsset() : this(GetDefaultLocators())
        {
        }

        public MoonAsset(IList<AssetLocator> locators)
        {
            AddAssetLocators(locators);
        }

        /// <summary>
        /// MoonAsset 默认 AssetLocator 加载列表
        /// </summary>
        public static IList<AssetLocator> GetDefaultLocators()
        {
            var locators = new List<AssetLocator>();

            // 1. 编辑器模式直接加载源文件
            // 2. 模拟模式，优先加载打包目录
#if UNITY_EDITOR
            if (MoonAsset.s_Mode == MoonAsset.EMode.Simulate)
            {
                locators.Add(new LocalAssetLocator(MoonAssetConfig.k_Editor_DlcOutputPath));
            }
#endif

            // 1.先加载dlc目录
            locators.Add(new LocalAssetLocator(MoonAssetConfig.k_DlcPath));
            // 2.再加载base目录
            locators.Add(new LocalAssetLocator(MoonAssetConfig.k_BasePath));
            // 3.都没有就下载到dlc目录，再加载
            locators.Add(new RemoteAssetLocator(MoonAssetConfig.k_DlcPath));

            return locators;
        }

        /// <summary>
        /// 资源路径定位器
        /// </summary>
        /// <param name="assetName">资源相对路径</param>
        /// <param name="assetPath">资源完整路径</param>
        /// <param name="remoteAssets">远端资源对象</param>
        /// <returns>false代表本地没有找到资源</returns>
        public delegate bool AssetLocator(string assetName, ref string assetPath, ref IRemoteAssets remoteAssets);

        private readonly List<AssetLocator> m_AssetLocators = new();

        public void AddAssetLocators(IList<AssetLocator> locators)
        {
            foreach (var locator in locators)
            {
                AddAssetLocator(locator);
            }
        }

        public void AddAssetLocator(AssetLocator locator)
        {
            m_AssetLocators.Add(locator);
        }

        /// <summary>
        /// 获取Manifest管理的资源路径
        /// </summary>
        /// <param name="assetName">相对路径资源名 eg. Bundles/xxx.assets</param>
        /// <param name="filePath">资源物理地址，绝对路径</param>
        /// <param name="remoteAssets">远端资源信息</param>
        /// <returns>本地是否存在</returns>
        public bool TryGetAssetPath(string assetName, out string filePath, out IRemoteAssets remoteAssets)
        {
            filePath = null;
            remoteAssets = null;

            // bundle 编辑器模式下 根本不会 进来

            for (int i = 0; i < m_AssetLocators.Count; i++)
            {
                var locator = m_AssetLocators[i];
                bool result = locator(assetName, ref filePath, ref remoteAssets);

                if (result)
                    return true;
            }

            if (remoteAssets == null)
                ERROR($"TryGetAssetPath failed. AssetName: {assetName}");

            return false;
        }
    }
}