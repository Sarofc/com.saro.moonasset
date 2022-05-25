using System;

namespace Saro.MoonAsset
{
    [Serializable]
    public class RawBundleRef : IRemoteAssets
    {
        /// <summary>
        /// 资源索引名称，eg.  Extra/dir/name.  <see cref="MoonAssetConfig.k_RawFolder"/>
        /// </summary>
        public string name;

        /// <summary>
        /// 长度
        /// </summary>
        public long size;

        /// <summary>
        /// TODO 压缩后的长度，没压缩则为0
        /// </summary>
        public long compressSize;

        /// <summary>
        /// (md5)
        /// </summary>
        public string hash;

        public override string ToString()
        {
            return $"{name}|{size}|{hash}";
        }

        string IRemoteAssets.Name => name;

        long IRemoteAssets.Size => size;

        long IRemoteAssets.CompressSize => compressSize;

        string IRemoteAssets.Hash => hash;

    }
}