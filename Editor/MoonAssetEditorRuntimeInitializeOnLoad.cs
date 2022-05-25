using UnityEngine;

namespace Saro.MoonAsset.Build
{
    public static class MoonAssetEditorRuntimeInitializeOnLoad
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void OnInitialize()
        {
            MoonAsset.s_Mode = BuildScript.GetSettings().runtimeMode;
        }
    }
}