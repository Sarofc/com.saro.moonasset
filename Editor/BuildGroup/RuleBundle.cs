using System;

namespace Saro.MoonAsset.Build
{
    [Serializable]
    public class RuleBundle
    {
        /// <summary>
        /// bundle名称
        /// </summary>
        public string bundle;

        /// <summary>
        /// 资源路径合集
        /// </summary>
        public string[] assets;

        /// <summary>
        /// 是否为rawfile，rawfile不支持依赖关系
        /// </summary>
        public bool isRawFile;
    }
}