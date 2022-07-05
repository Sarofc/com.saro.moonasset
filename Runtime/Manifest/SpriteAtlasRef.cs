using System;

namespace Saro.MoonAsset
{
    [Serializable]
    public class SpriteAtlasRef
    {
        /// <summary>
        /// sprite 名称
        /// </summary>
        public string sprite;

        /// <summary>
        /// sprite路径索引<see cref="Manifest.dirs"/>
        /// </summary>
        public int dirSprite;

        /// <summary>
        /// atlas 名称
        /// </summary>
        public string atlas;

        /// <summary>
        ///  atlas路径索引<see cref="Manifest.dirs"/>
        /// </summary>
        public int dirAtlas;
    }
}