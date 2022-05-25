namespace Saro.MoonAsset
{
    /// <summary>
    /// 资源定位器，用于获取 资源路径
    /// </summary>
    public abstract class AssetLocatorBase
    {
        protected readonly string m_Directory;

        public AssetLocatorBase(string directory)
        {
            m_Directory = directory;
        }

        /// <summary>
        /// 获取资源路径
        /// </summary>
        /// <param name="assetName">资源名</param>
        /// <param name="assetPath">资源本地路径</param>
        /// <param name="remoteAssets">远端资源信息</param>
        /// <returns>本地是否存在指定资源</returns>
        protected abstract bool GetAssetPath(string assetName, ref string assetPath, ref IRemoteAssets remoteAssets);

        public static implicit operator MoonAsset.AssetLocator(AssetLocatorBase locator)
        {
            return locator.GetAssetPath;
        }
    }
}
