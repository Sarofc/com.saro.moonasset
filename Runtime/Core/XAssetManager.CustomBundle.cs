using Cysharp.Threading.Tasks;
using Saro.IO;
using Saro.Net;
using System.Collections.Generic;
using System.IO;

namespace Saro.XAsset
{
    public partial class XAssetManager
    {
        public IReadOnlyDictionary<string, CustomBundleRef> CustomAssetMap => m_Manifest.CustomAssetMap;

        public byte[] LoadCustomAsset(string assetName)
        {
            if (CustomAssetMap.TryGetValue(assetName, out var bundle))
            {
                if (TryGetAssetPath(bundle.name, out var fullPath, out _))
                {
                    using (var vfs = VFileSystem.Open(fullPath, FileMode.Open, FileAccess.Read))
                    {
                        return vfs.ReadFile(assetName);
                    }
                }
            }

            ERROR($"LoadCustomAsset: {assetName} not found");

            return null;
        }

        public async UniTask<byte[]> LoadCustomAssetAsync(string assetName)
        {
            if (CustomAssetMap.TryGetValue(assetName, out var bundle))
            {
                var fullPath = await CheckCustomBundlesAsync(bundle.name);
                using (var vfs = VFileSystem.Open(fullPath, FileMode.Open, FileAccess.Read))
                {
                    return vfs.ReadFile(assetName);
                }
            }

            return null;
        }

        public async UniTask<string> CheckCustomBundlesAsync(string bundleName)
        {
            INFO($"<color=green>CheckCustomBundles</color>: {bundleName}");

            if (!TryGetAssetPath(bundleName, out var assetPath, out var remoteAssets))
            {
                if (remoteAssets == null)
                {
                    WARN($"remoteAsset is null. can't download from remote. url: {assetPath} path: {bundleName}");
                    return null;
                }

                OnLoadRemoteAsset?.Invoke(assetPath, false);

                var downloadUrl = XAssetConfig.GetRemoteAssetURL(remoteAssets.Name);

                int maxRetry = XAssetConfig.s_MaxDownloadRetryCount;
                var retry = maxRetry;

                bool downloadSuccess = false;

                while (retry-- > 0)
                {
                    var downloadAgent = Downloader.DownloadAsync(new DownloadInfo
                    {
                        DownloadUrl = downloadUrl,
                        SavePath = assetPath,
                        Size = remoteAssets.Size,
                        Hash = remoteAssets.Hash,
                    });

                    await downloadAgent;

                    if (downloadAgent.Status == EDownloadStatus.Success)
                    {
                        downloadSuccess = true;
                        break;
                    }

                    await UniTask.Delay(500); // 0.5s 后再试
                    WARN($"[auto] retry download ({retry}/{maxRetry}): {downloadAgent.Info.DownloadUrl}");
                }

                if (!downloadSuccess)
                {
                    OnLoadRemoteAssetError?.Invoke(downloadUrl);
                }

                OnLoadRemoteAsset?.Invoke(assetPath, true);
            }

            return assetPath;
        }
    }
}
