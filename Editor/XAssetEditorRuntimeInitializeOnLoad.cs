#if UNITY_EDITOR

using UnityEngine;

namespace Saro.XAsset.Build
{
    public static class XAssetEditorRuntimeInitializeOnLoad
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void OnInitialize()
        {
            XAssetManager.s_Mode = BuildScript.GetSettings().runtimeMode;
        }
    }
}

#endif