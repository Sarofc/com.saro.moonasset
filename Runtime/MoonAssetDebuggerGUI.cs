using System.Collections.Generic;
using System.IO;
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
            GUILayout.Label($"m_LoadingAssetHandles Count: {moonAsset.m_LoadingRequests.Count}");
            GUILayout.Label($"m_UnusedAssetHandles Count: {moonAsset.m_UnusedRequests.Count}");
            GUILayout.Label($"m_SceneHandles Count: {moonAsset.m_SceneRequests.Count}");

            GUILayout.Space(10);
            GUILayout.Label($"m_LoadingBundleHandles Count: {moonAsset.m_LoadingBundleRequests.Count}");
            GUILayout.Label($"m_PendingBundleHandles Count: {moonAsset.m_PendingBundleRequests.Count}");
            GUILayout.Label($"m_UnusedBundleHandles Count: {moonAsset.m_UnusedBundleRequests.Count}");

            m_Scroll = GUILayout.BeginScrollView(m_Scroll);

            foreach (var handle in moonAsset.m_LoadingRequests)
            {
                GUILayout.Label($"{handle.GetType().Name}: {Path.GetFileName(handle.AssetUrl)}");
                GUILayout.HorizontalSlider(handle.Progress, 0f, 1f);
            }

            foreach (var handle in moonAsset.m_UnusedRequests)
            {
                GUILayout.Label($"{handle.GetType().Name}: {Path.GetFileName(handle.AssetUrl)}");
            }

            foreach (var handle in moonAsset.m_SceneRequests)
            {
                GUILayout.Label($"{handle.GetType().Name}: {Path.GetFileName(handle.AssetUrl)}");
                GUILayout.HorizontalSlider(handle.Progress, 0f, 1f);
            }

            foreach (var handle in moonAsset.m_LoadingBundleRequests)
            {
                GUILayout.Label($"{handle.GetType().Name}: {Path.GetFileName(handle.AssetUrl)}");
                GUILayout.HorizontalSlider(handle.Progress, 0f, 1f);
            }

            foreach (var handle in moonAsset.m_PendingBundleRequests)
            {
                GUILayout.Label($"{handle.GetType().Name}: {Path.GetFileName(handle.AssetUrl)}");
            }

            foreach (var handle in moonAsset.m_UnusedBundleRequests)
            {
                GUILayout.Label($"{handle.GetType().Name}: {Path.GetFileName(handle.AssetUrl)}");
            }

            GUILayout.EndScrollView();
        }
    }
}