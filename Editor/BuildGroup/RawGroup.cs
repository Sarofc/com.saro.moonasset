using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Saro.MoonAsset.Build
{
    [Serializable]
    public class RawGroup
    {
        [Tooltip("分组名称，用于vfs文件名，appendhash时，会追加hash")]
        public string groupName;

        [Saro.Attributes.ReadOnly]
        public string tip = "加载路径为 {groupName}/{assets[0]}";

        [Tooltip("搜索通配符，多个之间请用,(逗号)隔开")]
        public string searchPattern = "*";

        [Tooltip("打包路径，相对路径，unity工程目录")]
        public string[] searchPaths;

        [Tooltip("包体资源")]
        public bool builtIn;

        [Tooltip("TODO 压缩")]
        public bool compress;

        [Tooltip("不将assetname记入manifest")]
        public bool disableAssetName;

        [Serializable]
        public class RuleRawAsset
        {
            public string name;
            public int dir;
        }

        [HideInInspector]
        public List<RuleRawAsset> assets = new List<RuleRawAsset>();

        public string GetCustomBundleName(Manifest manifest)
        {
            foreach (var asset in assets)
            {
                var name = groupName + "/" + asset.name;
                if (manifest.RawAssetMap.TryGetValue(name, out var bundle))
                {
                    return bundle.name;
                }
            }

            Log.ERROR("GetCustomBundleName error. group: " + groupName);

            return null;
        }

        public List<RuleRawAsset> GetAssets()
        {
            assets.Clear();
            for (int i = 0; i < searchPaths.Length; i++)
            {
                var searchPath = searchPaths[i];

                if (File.Exists(searchPath)) // searchPath 就是路径的情况，不会存在
                {
                    throw new Exception("searchPath must be folder");
                }
                else // searchPath 是文件夹的情况
                {
                    var originAssets = GetDirectoryFiles(searchPath);
                    for (int j = 0; j < originAssets.Length; j++)
                    {
                        string originAsset = originAssets[j];

                        originAsset = originAsset.Remove(0, searchPath.Length + 1);

                        assets.Add(new RuleRawAsset
                        {
                            name = originAsset,
                            dir = i,
                        });
                    }
                }
            }

            return assets;
        }

        private string[] GetDirectoryFiles(string searchPath)
        {
            var patterns = searchPattern.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (!Directory.Exists(searchPath))
            {
                Debug.LogError("Rule searchPath not exist:" + searchPath);
                return new string[0];
            }

            var getFiles = new List<string>(256);
            foreach (var item in patterns)
            {
                var files = Directory.GetFiles(searchPath, item, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (Directory.Exists(file)) continue; // ？

                    //var ext = Path.GetExtension(file).ToLower();

                    // 自定义打包暂时没有限制
                    //if (ext == ".fbx" && !item.Contains(ext)) continue;
                    //if (!BuildGroups.ValidateAsset(file)) continue; 

                    var asset = file.Replace("\\", "/");
                    getFiles.Add(asset);
                }
            }

            return getFiles.ToArray();
        }
    }
}