using UnityEditor;
using UnityEditor.Callbacks;

namespace Saro.MoonAsset.Build
{
    internal class PostBuildMethods
    {
        [PostProcessBuild(1)]
        private static void TestMethod(BuildTarget BuildTarget, string path)
        {
            // TODO 打包完成，一些回调可以放在这里

        }
    }
}
