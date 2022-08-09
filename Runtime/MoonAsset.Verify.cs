using Saro.Utility;
using System;
using System.IO;

namespace Saro.MoonAsset
{
    public partial class MoonAsset
    {
        public struct VerifyProgressData
        {
            public float percent;
            public string fileName;

            public VerifyProgressData(float percent, string fileName)
            {
                this.percent = percent;
                this.fileName = fileName;
            }
        }

        public string VerifyAllAssetsUseManifest(Action<VerifyProgressData> progress = null)
        {
            string result = null;

            if (Manifest != null)
            {
                var asset2Bundles = AssetToBundle;
                var index = 0;
                var count = asset2Bundles.Count;
                foreach (var kv in asset2Bundles)
                {
                    var assetBundleName = kv.Value.name;
                    var assetBundleHash = kv.Value.hash;

                    try
                    {
                        progress?.Invoke(new VerifyProgressData((float)index / count, assetBundleName));
                    }
                    catch (Exception e)
                    {
                        ERROR(e.ToString());
                    }

                    if (TryGetAssetPath(assetBundleName, out var bundlePath, out var remoteAssets))
                    {
                        if (FileUtility.IsAndroidStreammingAssetPath(bundlePath))
                        {
                            // TODO 安卓streammingasset暂时没有文件流接口，先跳过
                            // 原则上 是不是可以默认 安卓包体内的资源是不会丢的？
                        }
                        else
                        {
                            // 校验hash
                            using (var fs = new FileStream(assetBundleHash, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                var hash = HashUtility.GetMd5HexHash(fs);
                                if (!HashUtility.VerifyMd5HexHash(assetBundleHash, hash))
                                {
                                    result = ($"verify {assetBundleName}'s hash failed. reason: md5 missmatch [{assetBundleHash}] [{hash}]");
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        result = ($"verify {assetBundleName} failed. reason: file not found.");
                        break;
                    }

                    index++;
                }
            }
            else
            {
                result = ("manifest == null，can't verify assets");
            }

            return result;
        }
    }
}
