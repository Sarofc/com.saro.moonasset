using System;

//sbp 不支持变体
//https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@1.5/manual/UpgradeGuide.html

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
    }
}