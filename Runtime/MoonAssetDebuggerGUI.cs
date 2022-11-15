using System.Collections.Generic;
using UnityEngine;

namespace Saro.MoonAsset
{
    public sealed class MoonAssetDebuggerGUI : MonoBehaviour
    {
        public Rect windowRect = new Rect(20, 20, 450, 1000); // TODO 这个可能要自适应一下

        private Vector2 m_Scroll;

        private void OnGUI()
        {
            windowRect = GUI.Window(0, windowRect, DoMyWindow, "MoonAsset");
        }

        private void DoMyWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            var moonAsset = MoonAsset.Current;
            GUILayout.Label($"m_LoadingAssetHandles Count: {moonAsset.m_LoadingAssetHandles.Count}");
            GUILayout.Label($"m_UnusedAssetHandles Count: {moonAsset.m_UnusedAssetHandles.Count}");
            GUILayout.Label($"m_SceneHandles Count: {moonAsset.m_SceneHandles.Count}");

            GUILayout.Space(10);
            GUILayout.Label($"m_LoadingBundleHandles Count: {moonAsset.m_LoadingBundleHandles.Count}");
            GUILayout.Label($"m_PendingBundleHandles Count: {moonAsset.m_PendingBundleHandles.Count}");
            GUILayout.Label($"m_UnusedBundleHandles Count: {moonAsset.m_UnusedBundleHandles.Count}");

            m_Scroll = GUILayout.BeginScrollView(m_Scroll);

            foreach (var handle in moonAsset.m_LoadingAssetHandles)
            {
                GUILayout.Label($"{handle.GetType().Name}: {handle.AssetUrl}");
                GUILayout.HorizontalSlider(handle.Progress, 0f, 1f);
            }

            foreach (var handle in moonAsset.m_UnusedAssetHandles)
            {
                GUILayout.Label($"{handle.GetType().Name}: {handle.AssetUrl}");
            }

            foreach (var handle in moonAsset.m_SceneHandles)
            {
                GUILayout.Label($"{handle.GetType().Name}: {handle.AssetUrl}");
                GUILayout.HorizontalSlider(handle.Progress, 0f, 1f);
            }

            foreach (var handle in moonAsset.m_LoadingBundleHandles)
            {
                GUILayout.Label($"{handle.GetType().Name}: {handle.AssetUrl}");
                GUILayout.HorizontalSlider(handle.Progress, 0f, 1f);
            }

            foreach (var handle in moonAsset.m_PendingBundleHandles)
            {
                GUILayout.Label($"{handle.GetType().Name}: {handle.AssetUrl}");
            }

            foreach (var handle in moonAsset.m_UnusedBundleHandles)
            {
                GUILayout.Label($"{handle.GetType().Name}: {handle.AssetUrl}");
            }

            GUILayout.EndScrollView();
        }
    }
}