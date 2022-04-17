using Saro.Attributes;
using Saro.Pool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

//sbp 不支持变体
//https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@1.5/manual/UpgradeGuide.html

namespace Saro.XAsset.Build
{
    [Serializable]
    public class BundleGroup
    {
        public enum ENameBy
        {
            /// <summary>
            /// 显示指定包名
            /// </summary>
            [Obsolete("没用", true)]
            Explicit,
            /// <summary>
            /// 以路径为包名
            /// </summary>
            Path,
            /// <summary>
            /// 以目录为包名
            /// </summary>
            Directory,
        }

        [Tooltip("搜索路径")]
        [AssetPath(typeof(UnityEngine.Object), true)]
        public string searchPath;

        [Tooltip("搜索通配符，多个之间请用,(逗号)隔开")]
        public string searchPattern;

        [Tooltip("命名规则")]
        public ENameBy nameBy = ENameBy.Directory;

        [Tooltip("是否为BuiltIn资源")]
        public bool builtIn = false;

        [Tooltip("实际的资源列表")]
        [ReadOnly]
        [HideInInspector]
        public string[] assets = new string[0];

        public IList<string> GetAssetBundles(Manifest manifest)
        {
            using (HashSetPool<string>.Rent(out var set))
            {
                foreach (var asset in assets)
                {
                    if (manifest.AssetToBundle.TryGetValue(asset, out var bundle))
                    {
                        set.Add(bundle.name);

                        foreach (var dep in bundle.deps)
                        {
                            set.Add(manifest.bundles[dep].name); // 返回依赖
                        }
                    }
                }

                return set.ToArray();
            }
        }

        /// <returns>根据搜索规则,获取所有资源的路径</returns>
        public string[] GetAssets()
        {
            if (nameBy == ENameBy.Path)
            {
                if (File.Exists(searchPath)) // searchPath 就是路径的情况
                {
                    var ext = Path.GetExtension(searchPath).ToLower();
                    if (ext == ".fbx") return new string[0];
                    if (!BuildGroups.ValidateAsset(searchPath)) return new string[0];
                    var asset = searchPath.Replace("\\", "/");
                    assets = new string[] { asset };
                }
                else // searchPath 是文件夹的情况
                {
                    assets = GetDirectoryFiles();
                }
            }
            else if (nameBy == ENameBy.Directory)
            {
                assets = GetDirectoryFiles();
            }
            else
            {
                throw new NotImplementedException($"type {nameBy} not handled.");
            }

            return assets;
        }

        private string[] GetDirectoryFiles()
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

                    var ext = Path.GetExtension(file).ToLower();
                    if (ext == ".fbx" && !item.Contains(ext)) continue;
                    if (!BuildGroups.ValidateAsset(file)) continue;
                    var asset = file.Replace("\\", "/");
                    getFiles.Add(asset);
                }
            }

            return getFiles.ToArray();
        }
    }
}