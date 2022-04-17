using System;

namespace Saro.XAsset
{
    // TODO CustomAssetRef 提供压缩功能?
    // name 不能包含hash，因为 此结构，没有assetPath映射到bundle的过程，而是采用直接读取vfs文件，vfs文件可能直接包含hash
    [Serializable]
    public class CustomBundleRef : IRemoteAssets
    {
        /// <summary>
        /// 资源索引名称，eg.  Extra/dir/name.  <see cref="XAssetConfig.k_CustomFolder"/>
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