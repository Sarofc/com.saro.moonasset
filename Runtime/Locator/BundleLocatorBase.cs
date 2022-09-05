namespace Saro.MoonAsset
{
    /// <summary>
    /// 资源定位器，用于获取 资源路径
    /// </summary>
    public abstract class BundleLocatorBase
    {
        protected readonly string m_Directory;

        public BundleLocatorBase(string directory)
        {
            m_Directory = directory;
        }

        /// <summary>
        /// 获取资源路径
        /// </summary>
        /// <param name="bundleName">资源名</param>
        /// <param name="filePath">资源本地路径</param>
        /// <param name="remoteAssets">远端资源信息</param>
        /// <returns>本地是否存在指定资源</returns>
        protected abstract bool GetBundlePath(string bundleName, ref string filePath, ref IRemoteAssets remoteAssets);

        public static implicit operator MoonAsset.BundleLocator(BundleLocatorBase locator)
        {
            return locator.GetBundlePath;
        }
    }
}
