using Saro.SaroEditor;
using System.Collections.Generic;
using UnityEditor;

namespace Saro.XAsset.Build
{
    [CustomEditor(typeof(Settings))]
    public class XAssetSettingsInspector : BaseEditor<Settings>
    {
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
        }
    }
}