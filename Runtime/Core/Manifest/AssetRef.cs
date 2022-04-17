using System;

namespace Saro.XAsset
{
    [Serializable]
    public class AssetRef
    {
        /// <summary>
        /// 资源名
        /// </summary>
        public string name;

        /// <summary>
        /// AB包索引<see cref="Manifest.bundles"/>
        /// </summary>
        public int bundle;

        /// <summary>
        /// 文件路径索引<see cref="Manifest.dirs"/>
        /// </summary>
        public int dir;
    }
}