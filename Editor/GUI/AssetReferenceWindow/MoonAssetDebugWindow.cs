
using System.Collections.Generic;
using Saro.SEditor;
using UnityEditor;
using UnityEngine;

namespace Saro.MoonAsset
{
    public class MoonAssetDebugWindow : TabWindowContainer
    {
        [MenuItem("MGF Tools/Debug/MoonAssets Debugger")]
        private static void ShowWindow()
        {
            var window = GetWindow<MoonAssetDebugWindow>();
            window.titleContent = new GUIContent("MoonAssets Debugger");
            window.Show();
        }

        protected override void AddTabs()
        {
            AddTab(new MoonAssetReferenceTab());
        }
    }
}