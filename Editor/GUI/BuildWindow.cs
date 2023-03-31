using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Saro.MoonAsset.Build
{
    public sealed class BuildWindow : EditorWindow
    {
        [MenuItem("MGF Tools/Build")]
        private static void ShowBuildWindow()
        {
            var window = GetWindow<BuildWindow>();
            window.titleContent = new GUIContent("MGF-Build");
            window.Show();
        }

        private class Styles
        {
            public readonly GUIStyle style_FontItalic;
            public readonly GUIStyle style_FontBlodAndItalic;

            public Styles()
            {
                style_FontItalic = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Italic,
                    richText = true,
                };

                style_FontBlodAndItalic = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.BoldAndItalic,
                    richText = true,
                };
            }
        }

        private static Styles s_Styles;

        private void OnEnable()
        {
            EnsureMoonAssetSettings();
            EnsureBuildMethods();
        }

        private void OnGUI()
        {
            if (s_Styles == null)
            {
                s_Styles = new Styles();
            }

            DrawToolBar();
        }

        private void DrawToolBar()
        {
            m_Selected = GUILayout.Toolbar(m_Selected, s_Toolbar);
            m_ScrolPos = EditorGUILayout.BeginScrollView(m_ScrolPos);
            switch (m_Selected)
            {
                case 0:
                    DrawBuildSettings();
                    EditorGUILayout.Space();
                    DrawButtons();
                    EditorGUILayout.Space();
                    DrawBuildOptions();
                    break;
                default:
                    break;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawBuildSettings()
        {
            GUILayout.BeginVertical("helpbox");
            {
                EditorGUILayout.LabelField("Platform: " + EditorUserBuildSettings.activeBuildTarget, s_Styles.style_FontBlodAndItalic);

                switch (EditorUserBuildSettings.activeBuildTarget)
                {
                    case BuildTarget.StandaloneOSX:
                    case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
                        EditorGUILayout.LabelField("Scripting Backend: " + PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone));
                        break;
                    case BuildTarget.iOS:
                        EditorGUILayout.LabelField("Scripting Backend: " + PlayerSettings.GetScriptingBackend(BuildTargetGroup.iOS));
                        break;
                    case BuildTarget.Android:
                        EditorGUILayout.LabelField("Scripting Backend: " + PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android));
                        break;
                    case BuildTarget.WebGL:
                        EditorGUILayout.LabelField("Scripting Backend: " + PlayerSettings.GetScriptingBackend(BuildTargetGroup.WebGL));
                        break;
                    default:
                        EditorGUILayout.LabelField("Scripting Backend: Not Iml");
                        break;
                }

                EditorUserBuildSettings.development = EditorGUILayout.Toggle("Devepment Build: ", EditorUserBuildSettings.development);
                //if (EditorUserBuildSettings.development)
                //{
                //    EditorGUI.indentLevel++;
                //    EditorUserBuildSettings.connectProfiler = EditorGUILayout.Toggle("Connect Profiler: ", EditorUserBuildSettings.connectProfiler);
                //    EditorUserBuildSettings.allowDebugging = EditorGUILayout.Toggle("Script Debugging: ", EditorUserBuildSettings.allowDebugging);
                //    EditorUserBuildSettings.buildScriptsOnly = EditorGUILayout.Toggle("Build Scripts Only: ", EditorUserBuildSettings.buildScriptsOnly);
                //    EditorGUI.indentLevel--;
                //}
            }
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            if (m_Settings != null)
            {
                Editor.CreateCachedEditor(m_Settings, typeof(SettingsInspector), ref m_CachedEditor);
                if (m_CachedEditor != null)
                {
                    m_CachedEditor.OnInspectorGUI();
                }
            }
        }

        private void DrawBuildOptions()
        {
            GUILayout.BeginVertical("box");

            if (m_BuildMethods != null)
            {
                var rect = EditorGUILayout.GetControlRect();

                EditorGUI.LabelField(rect, "Build Pass：");

                rect.x = rect.width - 100f;
                rect.width = 100f;

                if (GUI.Button(rect, "Build Selected"))
                {
                    ExecuteAction(() =>
                    {
                        for (int i = 0; i < m_BuildMethods.Count; i++)
                        {
                            var buildMethod = m_BuildMethods[i];
                            if ((m_Settings.buildMethodFlag & (1 << i)) != 0 || buildMethod.required)
                            {
                                if (buildMethod.callback.Invoke() == false)
                                {
                                    throw new System.Exception($"Execute [{buildMethod.order}] {buildMethod.displayName} Failed, Abort！");
                                }
                            }
                        }
                    });
                }

                for (int i = 0; i < m_BuildMethods.Count; i++)
                {
                    DrawBuildMethod(i, m_BuildMethods[i]);
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawBuildMethod(int index, BuildMethod buildMethod)
        {
            if (buildMethod != null)
            {
                var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 8);
                EditorGUI.HelpBox(rect, string.Empty, MessageType.None);

                var rect1 = rect;
                rect1.y += 4f;
                rect1.x += 10f;
                rect1.width = 25f;

                buildMethod.selected = (m_Settings.buildMethodFlag & (1 << index)) != 0 || buildMethod.required;

                var tmpEnable = GUI.enabled;
                GUI.enabled = !buildMethod.required;
                buildMethod.selected = EditorGUI.ToggleLeft(rect1, string.Empty, buildMethod.selected);
                GUI.enabled = tmpEnable;

                if (buildMethod.selected) m_Settings.buildMethodFlag |= 1 << index;
                else m_Settings.buildMethodFlag &= ~(1 << index);
                EditorUtility.SetDirty(m_Settings); // fix buildMethodFlag 未保存

                rect1.x += rect1.width;
                rect1.width = 40f;
                rect1.height = EditorGUIUtility.singleLineHeight;

                if (GUI.Button(rect1, "Run"))
                {
                    ExecuteAction(() =>
                    {
                        if (buildMethod.callback.Invoke() == false)
                        {
                            EditorUtility.DisplayDialog("Failed", string.Format("Execute {0} failed!", buildMethod.displayName), "OK");
                        }
                    });
                }

                rect1.x += 40f + 10f;
                rect1.width = 300f;
                var label = new GUIContent(string.Format("[{0:00}] {1}", buildMethod.order, buildMethod.displayName), buildMethod.tooltip);
                var labelStyle = buildMethod.required ? s_Styles.style_FontBlodAndItalic : s_Styles.style_FontItalic;
                EditorGUI.LabelField(rect1, label, labelStyle);
            }
        }

        private void DrawButtons()
        {
            GUILayout.BeginVertical("box");
            {
                GUILayout.Label("functions");

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Player Settings.."))
                {
                    SettingsService.OpenProjectSettings("Project/Player");
                }

                if (GUILayout.Button("BuildGroup.."))
                {
                    Selection.activeObject = BuildScript.GetBuildGroups();
                }

                if (GUILayout.Button(new GUIContent("Browser..", "打开AssetBundle浏览器")))
                {
                    AssetBundleBrowser.AssetBundleBrowserMain.ShowWindow();
                }

                if (GUILayout.Button("Run FileServer"))
                {
                    var fileServerScriptPath = "GameTools/FileServer/run.cmd";

                    if (System.IO.File.Exists(fileServerScriptPath))
                    {
                        Common.Cmder.Run(fileServerScriptPath);
                    }
                    else
                    {
                        Log.ERROR("找不到文件服务器执行脚本，请自行检查：" + fileServerScriptPath);
                    }
                }

                if (GUILayout.Button(new GUIContent("Scenes", "把工程里所有场景，都加在EditorSettings里")))
                {
                    var paths = GetAllScenes();

                    var scenes = new EditorBuildSettingsScene[paths.Length];

                    for (int i = 0; i < paths.Length; i++)
                    {
                        var path = paths[i];
                        scenes[i] = new EditorBuildSettingsScene(path, true);
                    }

                    EditorBuildSettings.scenes = scenes;
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private string[] GetAllScenes()
        {
            var sceneGUIDs = AssetDatabase.FindAssets("t:scene");

            var paths = new string[sceneGUIDs.Length];
            for (int i = 0; i < sceneGUIDs.Length; i++)
            {
                string guid = sceneGUIDs[i];
                paths[i] = AssetDatabase.GUIDToAssetPath(guid);
            }

            return paths;
        }

        private static string[] s_Toolbar = new string[]
        {
             "MoonAssetSettings"
        };
        private Editor m_CachedEditor;
        private List<BuildMethod> m_BuildMethods;
        private Settings m_Settings;
        private int m_Selected;
        private Vector2 m_ScrolPos;

        private void EnsureBuildMethods()
        {
            m_BuildMethods = BuildMethod.BuildMethodCollection;
        }

        private void EnsureMoonAssetSettings()
        {
            m_Settings = BuildScript.GetSettings();
            BuildScript.GetManifest();
            BuildScript.GetBuildGroups();
        }

        private void ExecuteAction(System.Action action)
        {
            EditorUtility.DisplayProgressBar("Wait...", "", 0);

            EditorApplication.delayCall = () =>
            {
                EditorApplication.delayCall = null;
                if (action != null)
                {
                    try
                    {
                        action();
                    }
                    finally
                    {
                        EditorUtility.ClearProgressBar();
                    }
                }
            };
        }
    }
}