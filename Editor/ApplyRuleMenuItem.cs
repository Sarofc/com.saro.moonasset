//using UnityEditor;

//namespace Saro.XAsset.Build
//{
//    // 已经可以已文件夹为单位了，这个没必要了，后面，还会增加gui操作，分配group
//    internal class ApplyRuleMenuItem
//    {
//        [MenuItem("Assets/Apply Rule/Text", false, 1)]
//        private static void ApplyRuleText()
//        {
//            var rules = BuildScript.GetBuildGroups();
//            AddRulesForSelection(rules, rules.searchPatternText);
//        }

//        [MenuItem("Assets/Apply Rule/Prefab", false, 1)]
//        private static void ApplyRulePrefab()
//        {
//            var rules = BuildScript.GetBuildGroups();
//            AddRulesForSelection(rules, rules.searchPatternPrefab);
//        }

//        [MenuItem("Assets/Apply Rule/PNG", false, 1)]
//        private static void ApplyRulePNG()
//        {
//            var rules = BuildScript.GetBuildGroups();
//            AddRulesForSelection(rules, rules.searchPatternPng);
//        }

//        [MenuItem("Assets/Apply Rule/Material", false, 1)]
//        private static void ApplyRuleMaterail()
//        {
//            var rules = BuildScript.GetBuildGroups();
//            AddRulesForSelection(rules, rules.searchPatternMaterial);
//        }

//        [MenuItem("Assets/Apply Rule/Controller", false, 1)]
//        private static void ApplyRuleController()
//        {
//            var rules = BuildScript.GetBuildGroups();
//            AddRulesForSelection(rules, rules.searchPatternController);
//        }

//        [MenuItem("Assets/Apply Rule/Asset", false, 1)]
//        private static void ApplyRuleAsset()
//        {
//            var rules = BuildScript.GetBuildGroups();
//            AddRulesForSelection(rules, rules.searchPatternAsset);
//        }

//        [MenuItem("Assets/Apply Rule/Scene", false, 1)]
//        private static void ApplyRuleScene()
//        {
//            var rules = BuildScript.GetBuildGroups();
//            AddRulesForSelection(rules, rules.searchPatternScene);
//        }

//        [MenuItem("Assets/Apply Rule/Dir", false, 1)]
//        private static void ApplyRuleDir()
//        {
//            var rules = BuildScript.GetBuildGroups();
//            AddRulesForSelection(rules, rules.searchPatternDir);
//        }

//        private static void AddRulesForSelection(BuildGroups rules, string searchPattern)
//        {
//            var isDir = rules.searchPatternDir.Equals(searchPattern);
//            foreach (var item in Selection.objects)
//            {
//                var path = AssetDatabase.GetAssetPath(item);
//                var rule = new BundleGroup()
//                {
//                    searchPath = path,
//                    searchPattern = searchPattern,
//                    nameBy = isDir ? BundleGroup.ENameBy.Directory : BundleGroup.ENameBy.Path
//                };
//                ArrayUtility.Add(ref rules.bundleGroups, rule);
//            }

//            EditorUtility.SetDirty(rules);
//            AssetDatabase.SaveAssets();
//        }
//    }
//}
