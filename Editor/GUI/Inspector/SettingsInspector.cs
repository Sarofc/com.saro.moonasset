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
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
}