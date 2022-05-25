namespace Saro.MoonAsset
{
    public partial class MoonAsset
    {
        /// <summary>
        /// XAset默认AssetLocator
        /// </summary>
        public void SetDefaultLocator()
        {
            // 1. 编辑器模式直接加载源文件
            // 2. 模拟模式，优先加载打包目录
#if UNITY_EDITOR
            if (MoonAsset.s_Mode == MoonAsset.EMode.Simulate)
            {
                AddAssetLocator(new LocalAssetLocator(MoonAssetConfig.k_Editor_DlcOutputPath));
            }
#endif

            // 1.先加载dlc目录
            AddAssetLocator(new LocalAssetLocator(MoonAssetConfig.k_DlcPath));
            // 2.再加载base目录
            AddAssetLocator(new LocalAssetLocator(MoonAssetConfig.k_BasePath));
            // 3.都没有就下载到dlc目录，再加载
            AddAssetLocator(new RemoteAssetLocator(MoonAssetConfig.k_DlcPath));
        }
    }
}
