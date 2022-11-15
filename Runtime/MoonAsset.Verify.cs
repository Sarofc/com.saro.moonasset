using Cysharp.Threading.Tasks;
using Saro.Net;
using Saro.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

        public string VerifyAllAssetsUseManifest(Action<VerifyProgressData> progress = null, List<DownloadInfo> downloadInfos = null)
        {
            if (downloadInfos != null) downloadInfos.Clear();

            var sb = new StringBuilder(1024);
            var num = 0;
            if (Manifest != null)
            {
                var asset2Bundles = AssetToBundle;
                var index = 0;
                var count = asset2Bundles.Count;
                foreach (var kv in asset2Bundles)
                {
                    var bundleName = kv.Value.name;
                    var bundleHash = kv.Value.hash;

                    try
                    {
                        progress?.Invoke(new VerifyProgressData((float)index / count, bundleName));
                    }
                    catch (Exception e)
                    {
                        ERROR(e.ToString());
                    }

                    bool exists = TryGetAssetPath(bundleName, out var bundlePath, out var remoteAssets);
                    if (exists)
                    {
                        if (FileUtility.ShouldUseUnityWebRequest(bundlePath))
                        {
                            // TODO 安卓/webgl streammingasset暂时没有文件流接口，先跳过
                            // 原则上 是不是可以默认 安卓包体内的资源是不会丢的？
                        }
                        else
                        {
                            // 校验hash
                            using (var fs = new FileStream(bundlePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                var hash = HashUtility.GetMd5HexHash(fs);
                                if (!HashUtility.VerifyMd5HexHash(bundleHash, hash))
                                {
                                    AddToDownloadInfos(downloadInfos, bundlePath, remoteAssets);

                                    sb.AppendLine(($"{bundleName}. hash missmatch."));
                                    num++;
                                }
                            }
                        }
                    }
                    else
                    {
                        AddToDownloadInfos(downloadInfos, bundlePath, remoteAssets);

                        sb.AppendLine(($"{bundleName}.  file not found."));
                        num++;
                    }

                    index++;
                }
            }
            else
            {
                sb.AppendLine(("manifest == null，can't verify assets"));
            }

            if (num > 0)
            {
                sb.Insert(0, $"num: {num}\n");
            }

            return sb.ToString();

            static void AddToDownloadInfos(List<DownloadInfo> infos, string savePath, IRemoteAssets remoteAssets)
            {
                if (remoteAssets == null)
                {
                    return;
                }

                infos?.Add(new DownloadInfo
                {
                    DownloadUrl = MoonAssetConfig.GetRemoteAssetURL(remoteAssets.Name),
                    SavePath = savePath,
                    Hash = remoteAssets.Hash,
                    Size = remoteAssets.Size,
                });
            }
        }

        /// <summary>
        /// TODO，此方法不要用。
        /// 应该是搞个 分包管理器，自动下载，或手动下载。
        /// </summary>
        /// <returns></returns>
        public async UniTask<bool> DownloadAllAssetsUseManifest()
        {
            var infos = new List<DownloadInfo>(1024);
            VerifyAllAssetsUseManifest(downloadInfos: infos);
            if (infos.Count > 0)
            {
                var tasks = new UniTask[infos.Count];
                for (int i = 0; i < infos.Count; i++)
                {
                    DownloadInfo info = infos[i];
                    var agent = Downloader.DownloadAsync(info);
                    tasks[i] = agent.ToUniTask();
                }

                await UniTask.WhenAll(tasks);

                infos.Clear();
                VerifyAllAssetsUseManifest(downloadInfos: infos);
            }

            return infos.Count == 0;
        }
    }
}
