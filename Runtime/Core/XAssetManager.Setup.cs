namespace Saro.XAsset
{
    public partial class XAssetManager
    {
        /// <summary>
        /// XAset默认AssetLocator
        /// </summary>
        public void SetDefaultLocator()
        {
            // 1. 编辑器模式直接加载源文件
            // 2. 模拟模式，优先加载打包目录
#if UNITY_EDITOR
            if (XAssetManager.s_Mode == XAssetManager.EMode.Simulate)
            {
                AddAssetLocator(new LocalAssetLocator(XAssetConfig.k_Editor_DlcOutputPath));
            }
#endif

            // 1.先加载dlc目录
            AddAssetLocator(new LocalAssetLocator(XAssetConfig.k_DlcPath));
            // 2.再加载base目录
            AddAssetLocator(new LocalAssetLocator(XAssetConfig.k_BasePath));
            // 3.都没有就下载到dlc目录，再加载
            AddAssetLocator(new RemoteAssetLocator(XAssetConfig.k_DlcPath));
        }
    }
}
