using System;

//sbp 不支持变体
//https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@1.5/manual/UpgradeGuide.html

namespace Saro.MoonAsset.Build
{
    [Serializable]
    public class RuleSprite
    {
        public string spritePath;

        public string atlasPath;
    }
}