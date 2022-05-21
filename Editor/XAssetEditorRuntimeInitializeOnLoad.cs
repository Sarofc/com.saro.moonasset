#if UNITY_EDITOR

using UnityEngine;

namespace Saro.XAsset.Build
{
    public static class XAssetEditorRuntimeInitializeOnLoad
    {
#if UNITY_2019_1_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void OnInitialize()
        {
            XAssetManager.s_Mode = BuildScript.GetSettings().runtimeMode;
        }
    }
}

#endif