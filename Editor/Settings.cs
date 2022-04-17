using System;
using UnityEditor;
using UnityEngine;

namespace Saro.XAsset.Build
{
    public class Settings : ScriptableObject
    {
        [Tooltip("是否在编辑器下开启加载AssetBundle的模式，开启后需要先打AssetBundle")]
        public XAssetManager.EMode runtimeMode = XAssetManager.EMode.Editor;

        [Tooltip("Assetbundle打包设置")]
        public BuildAssetBundleOptions buildAssetBundleOptions = BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.ChunkBasedCompression;

        [Tooltip("详细打包报告")]
        public bool detailBuildReport;

        [HideInInspector]
        public int buildMethodOptions;

        [Tooltip("打包宏，仅打包时传入打包脚本，英文逗号分割")]
        public string extraScriptingDefines;

        public string[] ExtraScriptingDefines => extraScriptingDefines.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
    }
}