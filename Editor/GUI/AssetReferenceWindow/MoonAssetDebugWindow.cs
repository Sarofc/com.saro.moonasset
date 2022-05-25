
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Saro.MoonAsset
{
    public class MoonAssetDebugWindow : EditorWindow
    {
        private List<TabWindow> m_TabWindows;
        private int m_Mode;
        private string[] m_ToolbarLabels;

        [MenuItem("MGF Tools/Debug/MoonAssets Debugger")]
        private static void ShowWindow()
        {
            var window = GetWindow<MoonAssetDebugWindow>();
            window.titleContent = new GUIContent("MoonAssets Debugger");
            window.Show();
        }

        private void OnEnable()
        {
            m_TabWindows = new List<TabWindow>();
            m_TabWindows.Add(new MoonAssetReferenceTab());

            m_ToolbarLabels = new string[m_TabWindows.Count];

            for (int i = 0; i < m_TabWindows.Count; i++)
            {
                TabWindow item = m_TabWindows[i];

                item.OnEnable();

                m_ToolbarLabels[i] = item.TabName;
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < m_TabWindows.Count; i++)
            {
                TabWindow item = m_TabWindows[i];

                item.OnDisable();
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("运行时查看", MessageType.Warning);
                return;
            }

            if (MoonAsset.Current == null)
            {
                EditorGUILayout.HelpBox("MoonAsset 实例未注册", MessageType.Warning);
                return;
            }

            m_Mode = GUILayout.Toolbar(m_Mode, m_ToolbarLabels, "LargeButton");

            m_TabWindows[m_Mode].OnGUI(GetSubWindowArea());
        }


        private Rect GetSubWindowArea()
        {
            float padding = 32;
            Rect subPos = new Rect(0, padding, position.width, position.height - padding);
            return subPos;
        }
    }
}