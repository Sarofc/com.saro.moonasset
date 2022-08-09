using Saro.Utility;

namespace Saro.MoonAsset
{
    public sealed class LocalAssetLocator : AssetLocatorBase
    {
        public LocalAssetLocator(string directory) : base(directory)
        {
        }

        protected override bool GetAssetPath(string assetName, ref string assetPath, ref IRemoteAssets remoteAssets)
        {
            //if (MoonAssetConfig.s_UseSubFolderForStorge)
            //{
            //    assetPath = MoonAssetConfig.GetCompatibleFileName(assetName);
            //    if (FileUtility.Exists(assetPath))
            //    {
            //        return true;
            //    }
            //}

            assetPath = m_Directory + "/" + assetName;
            if (FileUtility.Exists(assetPath))
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
