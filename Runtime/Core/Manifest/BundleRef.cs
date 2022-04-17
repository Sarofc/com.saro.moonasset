using System;

namespace Saro.XAsset
{
    [Serializable]
    public class BundleRef : IRemoteAssets
    {
        /// <summary>
        /// AB包名，eg.  Bundle/abName.  <see cref="XAssetConfig.k_AssetBundleFoler"/>
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

        /// <summary>
        /// 使用虚拟文件系统
        /// </summary>
        //public string fileSystem;

        public override string ToString()
        {
            return $"{name}|{size}|{hash}";
        }

        string IRemoteAssets.Name => name;

        long IRemoteAssets.Size => size;

        long IRemoteAssets.CompressSize => 0L; // assetbundle 不实现压缩，assetbundle自己管理吧

        string IRemoteAssets.Hash => hash;
    }
}