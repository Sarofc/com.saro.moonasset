using Cysharp.Threading.Tasks;
using Saro.IO;
using Saro.Net;
using System.IO;

namespace Saro.MoonAsset
{
    public partial class MoonAsset
    {
        public byte[] GetRawFile(string assetName)
        {
            if (AssetToBundle.TryGetValue(assetName, out var bundle))
            {
                if (TryGetAssetPath(bundle.name, out var fullPath, out _))
                {
                    using (var vfs = VFileSystem.Open(fullPath, FileMode.Open, FileAccess.Read))
                    {
                        return vfs.ReadFile(assetName);
                    }
                }
            }

            ERROR($"RawFile not found in manifest : {assetName}");
            return null;
        }

        public async UniTask<byte[]> GetRawFileAsync(string assetName)
        {
            var fullPath = await DownloadRawFileAsync(assetName);
            if (!string.IsNullOrEmpty(fullPath))
            {

                using (var vfs = VFileSystem.Open(fullPath, FileMode.Open, FileAccess.Read))
                {
                    return vfs.ReadFile(assetName);
                }
            }
            return null;
        }

        public async UniTask<string> DownloadRawFileAsync(string assetName)
        {
            INFO($"<color=green>CheckRawBundlesAsync</color>: {assetName}");

            if (!AssetToBundle.TryGetValue(assetName, out var bundle))
            {
                ERROR($"RawFile not found in manifest : {assetName}");
                return null;
            }

            var bundleName = bundle.name;

            if (!TryGetAssetPath(bundleName, out var filePath, out var remoteAssets))
            {
                if (remoteAssets == null)
                {
                    WARN($"remoteAsset is null. can't download from remote. url: {filePath} path: {assetName}");
                    return null;
                }

                OnLoadRemoteAsset?.Invoke(filePath, false);

                var downloadUrl = MoonAssetConfig.GetRemoteAssetURL(remoteAssets.Name);

                int maxRetry = MoonAssetConfig.s_MaxDownloadRetryCount;
                var retry = maxRetry;

                bool downloadSuccess = false;

                while (retry-- > 0)
                {
                    var downloadAgent = Downloader.DownloadAsync(new DownloadInfo
                    {
                        DownloadUrl = downloadUrl,
                        SavePath = filePath,
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

                OnLoadRemoteAsset?.Invoke(filePath, true);
            }

            return filePath;
        }
    }
}
