using Saro.Utility;

namespace Saro.XAsset
{
    public sealed class LocalAssetLocator : BaseAssetLocator
    {
        public LocalAssetLocator(string directory) : base(directory)
        {
        }

        protected override bool GetAssetPath(string assetName, ref string assetPath, ref IRemoteAssets remoteAssets)
        {
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
