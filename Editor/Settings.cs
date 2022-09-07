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

        [Tooltip("是否覆盖宏，目前只在 BuildScript.BuildPlayer() 里调用。\n谨慎使用，可能造成难以排查的bug!\n建议直接使用unity自带的宏定义配置")]
        [System.Obsolete("废弃了")]
        public bool overrideSymbols;

        [Tooltip("打包宏，完全覆盖，仅打包时传入打包脚本，英文逗号分割")]
        [System.Obsolete("废弃了")]
        public string[] scriptingDefineSymbols;
    }
}