namespace Saro.MoonAsset
{
    /// <summary>
    /// 定位远端资源，永远返回false
    /// </summary>
    public sealed class BundleLocator_Remote : BundleLocatorBase
    {
        public BundleLocator_Remote(string directory) : base(directory)
        {
        }

        protected override bool GetBundlePath(string bundleName, ref string filePath, ref IRemoteAssets remoteAssets)
        {
            var manifest = MoonAsset.Current?.Manifest;
            if (manifest != null && manifest.TryGetRemoteAsset(bundleName, out remoteAssets))
            {
                if (remoteAssets != null /*&& remoteAsset.Hash == fileInfo.Length*/)
                {
                    // 加载路径指向下载目录
                    filePath = MoonAssetConfig.k_DlcPath + "/" + bundleName;
                }
            }

            return false;
        }
    }
}
