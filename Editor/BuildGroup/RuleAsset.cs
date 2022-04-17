using System;

//sbp 不支持变体
//https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@1.5/manual/UpgradeGuide.html

namespace Saro.XAsset.Build
{
    [Serializable]
    public class RuleAsset
    {
        /// <summary>
        /// 资源路径
        /// </summary>
        public string asset;

        /// <summary>
        /// bundle名称
        /// </summary>
        public string bundle;
    }
}