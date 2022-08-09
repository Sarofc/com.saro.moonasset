using System;
using UnityEditor;
using UnityEngine;

namespace Saro.MoonAsset.Build
{
    public class Settings : ScriptableObject
    {
        [Tooltip("是否在编辑器下开启加载AssetBundle的模式，开启后需要先打AssetBundle")]
        public MoonAsset.EMode runtimeMode = MoonAsset.EMode.AssetDatabase;

        [Tooltip("AssetBundle打包设置")]
        public BuildAssetBundleOptions buildAssetBundleOptions = BuildAssetBundleOptions.DeterministicAssetBundle |
                                                                 BuildAssetBundleOptions.ChunkBasedCompression;

        [Tooltip("详细打包报告")]
        public bool detailBuildReport;

        [HideInInspector]
        [SerializeField]
        public int buildMethodFlag;

        [Tooltip("是否覆盖宏")]
        public bool overrideSymbols;

        [Tooltip("打包宏，完全覆盖，仅打包时传入打包脚本，英文逗号分割")]
        public string[] scriptingDefineSymbols;
    }
}