namespace Saro.MoonAsset
{
    public interface IRemoteAssets
    {
        /// <summary>
        /// 资源索引，虚拟寻址
        /// </summary>
        string Name { get; }
        /// <summary>
        /// 资源大小
        /// </summary>
        long Size { get; }
        /// <summary>
        /// 资源hash
        /// </summary>
        string Hash { get; }
        /// <summary>
        /// TODO 压缩后的长度，没压缩则为0
        /// </summary>
        //long CompressSize { get; }
        /// <summary>
        /// 是否被压缩
        /// </summary>
        //bool HasCompress => CompressSize > 0;
    }
}