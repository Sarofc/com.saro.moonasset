using Saro.Utility;

namespace Saro.MoonAsset
{
    public sealed class BundleLocator_Local : BundleLocatorBase
    {
        public BundleLocator_Local(string directory) : base(directory)
        {
        }

        protected override bool GetBundlePath(string bundleName, ref string filePath, ref IRemoteAssets remoteAssets)
        {
            //if (MoonAssetConfig.s_UseSubFolderForStorge)
            //{
            //    assetPath = MoonAssetConfig.GetCompatibleFileName(assetName);
            //    if (FileUtility.Exists(assetPath))
            //    {
            //        return true;
            //    }
            //}

            filePath = m_Directory + "/" + bundleName;
            if (FileUtility.Exists(filePath))
            {
                return true;
            }
            else
            {
                //Log.INFO("LocalAssetLocator failed. path: " + assetPath);
            }

            return false;
        }
    }
}
