using System;

namespace Saro.MoonAsset
{
    [Serializable]
    public class BundleRef : IRemoteAssets
    {
        /// <summary>
        /// AB包名，eg.  Bundle/abName.  <see cref="MoonAssetConfig.k_AssetBundleFoler"/>
        /// </summary>
        public string name;

        /// <summary>
        /// AB包依赖<see cref="Manifest.bundles"/>
        /// </summary>
        public int[] deps;

        /// <summary>
        /// 长度
        /// </summary>
        public long size;

        /// <summary>
        /// (md5)
        /// </summary>
        public string hash;

        public override string ToString() => $"{name}|{size}|{hash}";

        string IRemoteAssets.Name => name;

        long IRemoteAssets.Size => size;

        string IRemoteAssets.Hash => hash;
    }
}