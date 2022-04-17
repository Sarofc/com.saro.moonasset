namespace Saro.XAsset
{
    /// <summary>
    /// 资源定位器，用于获取 资源路径
    /// </summary>
    public abstract class BaseAssetLocator
    {
        protected string m_Directory;

        public BaseAssetLocator(string directory)
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

        public static implicit operator XAssetManager.AssetLocator(BaseAssetLocator locator)
        {
            return locator.GetAssetPath;
        }
    }
}
