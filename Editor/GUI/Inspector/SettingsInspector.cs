using System;
using Saro.SEditor;
using System.Collections.Generic;
using Saro.Utility;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Saro.MoonAsset.Build
{
    [CustomEditor(typeof(Settings))]
    public class SettingsInspector : BaseEditor<Settings>
    {
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);

            excluded.Add(TypeUtility.PropertyName(() => Target.overrideSymbols));
            excluded.Add(TypeUtility.PropertyName(() => Target.scriptingDefineSymbols));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("helpbox");
            {
                EditorGUILayout.LabelField("Script Compilation", EditorStyles.boldLabel);
                
                const float k_ButtonWidth = 48f;
                var rect = EditorGUILayout.GetControlRect();
                var propertyRect = rect;
                propertyRect.width = rect.width - k_ButtonWidth;
                EditorGUI.PropertyField(propertyRect,
                    serializedObject.FindProperty(() => Target.overrideSymbols));

                var buttonRect = rect;
                buttonRect.x += propertyRect.width;
                buttonRect.width = k_ButtonWidth;
                if (GUI.Button(buttonRect, "reset"))
                {
                    var namedBuildTarget =
                        NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

                    PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out Target.scriptingDefineSymbols);

                    EditorUtility.SetDirty(Target);
                    AssetDatabase.SaveAssets();
                }

                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty(() => Target.scriptingDefineSymbols));

            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}