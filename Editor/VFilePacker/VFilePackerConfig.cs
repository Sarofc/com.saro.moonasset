//using System;
//using System.Collections.Generic;
//using System.IO;
//using UnityEngine;

//namespace Saro.MoonAsset.Build
//{
//    public class VFilePackerConfig : ScriptableObject
//    {
//        [Serializable]
//        public class VFileInfo
//        {
//            [Tooltip("分组名称，用于vfs文件名，appendhash时，会追加hash")]
//            public string name;
//            [Saro.Attributes.ReadOnly]
//            public string tip = "加载路径为 {groupName}/{assets[0]}";
//            [Tooltip("搜索通配符，多个之间请用,(逗号)隔开")]
//            public string searchPattern = "*";
//            [Tooltip("打包路径，相对路径，unity工程目录")]
//            public string[] searchPaths;

//            [HideInInspector]
//            [SerializeField]
//            public List<string> assets = new List<string>();

//            public List<string> GetAssets()
//            {
//                assets.Clear();
//                for (int i = 0; i < searchPaths.Length; i++)
//                {
//                    var searchPath = searchPaths[i];
//                    if (File.Exists(searchPath)) // searchPath 就是路径的情况，不会存在
//                    {
//                        throw new Exception("searchPath must be folder");
//                    }
//                    else // searchPath 是文件夹的情况
//                    {
//                        var files = GetDirectoryFiles(searchPath);
//                        for (int j = 0; j < files.Length; j++)
//                        {
//                            string file = files[j];
//                            assets.Add(file);
//                        }
//                    }
//                }
//                return assets;
//            }

//            private string[] GetDirectoryFiles(string searchPath)
//            {
//                var patterns = searchPattern.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
//                if (!Directory.Exists(searchPath))
//                {
//                    Debug.LogError("Rule searchPath not exist:" + searchPath);
//                    return new string[0];
//                }
//                var getFiles = new List<string>(256);
//                foreach (var item in patterns)
//                {
//                    var files = Directory.GetFiles(searchPath, item, SearchOption.AllDirectories);
//                    foreach (var file in files)
//                    {
//                        if (Directory.Exists(file)) continue; // ?
//                        var asset = file.Replace("\\", "/");
//                        getFiles.Add(asset);
//                    }
//                }
//                return getFiles.ToArray();
//            }
//        }

//        public List<VFileInfo> vfiles = new();

//        public void Pack()
//        {
//            foreach (var vfile in vfiles)
//            {
                
//            }
//        }
//    }
//}
