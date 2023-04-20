using System;
using Saro.Net;
using UnityEngine;

namespace Saro.MoonAsset
{
    public sealed class BundleRequest_Remote : BundleRequest
    {
        private IDownloadAgent m_DownloadAgent;
        private AssetBundleCreateRequest m_AssetBundleCreateRequest;
        private bool m_IsWaitForCompletion;

        public DownloadInfo Info { get; set; }

        public int RetryCount { get; set; } = MoonAssetConfig.s_MaxDownloadRetryCount;

        public override string Error
        {
            get { return m_DownloadAgent != null && m_DownloadAgent.IsDone ? m_DownloadAgent.Error : null; }
        }

        public override bool IsDone
        {
            get
            {
                if (LoadState == ELoadState.Init)
                    return false;

                if (LoadState == ELoadState.Loaded)
                    return true;

                if (LoadState == ELoadState.Downloading)
                {
                    if (m_DownloadAgent.IsDone)
                    {
                        bool downloadFinish = false;

                        if (m_DownloadAgent.Status == EDownloadStatus.Success)
                        {
                            // 下载成功，加载bundle
                            StartLoadBundle();

                            downloadFinish = true;
                        }
                        else
                        {
                            if (--RetryCount >= 0)
                            {
                                StartDownload();
                            }
                            else
                            {
                                MoonAsset.Current.OnLoadRemoteAssetError?.Invoke(AssetUrl);

                                downloadFinish = true;
                            }
                        }

                        if (downloadFinish)
                        {
                            LoadState = ELoadState.LoadAssetBundle;
                        }
                    }
                }

                if (LoadState == ELoadState.LoadAssetBundle)
                {
                    if (m_IsWaitForCompletion)
                    {
                        Asset = m_AssetBundleCreateRequest.assetBundle; // 强制同步加载
                        MoonAsset.WARN($"[{nameof(BundleRequest_Local)}] sync load: {Asset}");
                        LoadState = ELoadState.Loaded;
                    }
                    else
                    {
                        if (m_AssetBundleCreateRequest.isDone)
                        {
                            Asset = m_AssetBundleCreateRequest.assetBundle;
                            LoadState = ELoadState.Loaded;
                        }
                    }
                }

                if (LoadState == ELoadState.Loaded)
                {
                    if (Bundle == null)
                        Error = $"[{nameof(BundleRequest_Local)}] load assetBundle failed. url: {AssetUrl}";

                    MoonAsset.Current.OnLoadRemoteAsset?.Invoke(Info.DownloadUrl, true);

                    return true;
                }

                return false;
            }
        }

        public override float Progress
        {
            get
            {
                var total = 2f;
                var progress = (m_DownloadAgent != null ? m_DownloadAgent.Progress : 0f)
                    + (m_AssetBundleCreateRequest != null ? m_AssetBundleCreateRequest.progress : 0f);

                return progress / total;
            }
        }

        internal override void Load()
        {
            StartDownload();

            MoonAsset.Current.OnLoadRemoteAsset?.Invoke(Info.DownloadUrl, false);

            LoadState = ELoadState.Downloading;
        }

        internal override void Unload(bool unloadAllObjects = true)
        {
            LoadState = ELoadState.Unload;

            if (Bundle == null)
                return;
            Bundle.Unload(unloadAllObjects);
            Bundle = null;

            m_IsWaitForCompletion = false;
        }

        private void StartLoadBundle()
        {
            m_AssetBundleCreateRequest = AssetBundle.LoadFromFileAsync(AssetUrl);
        }

        private void StartDownload()
        {
            m_DownloadAgent = Downloader.DownloadAsync(Info);
        }

        public override void WaitForCompletion()
        {
            m_IsWaitForCompletion = true;

            if (MoonAssetConfig.PlatformUsesMultiThreading(Application.platform))
            {
                const float max_timeout = 10f; // 同步加载，默认超时时间
                float timeout = max_timeout + Time.realtimeSinceStartup;
                while (!IsDone)
                {
                    if (timeout <= Time.realtimeSinceStartup)
                    {
                        MoonAsset.ERROR($"sync load timeout: {max_timeout}s, try async load instead. {AssetUrl}");
                        break;
                    }

                    Downloader.OnUpdate();
                }
            }
            else
            {
                throw new PlatformNotSupportedException($"[MoonAsset] {Application.platform} not support sync load");
            }

#if DEBUG
            if (!IsDone)
                MoonAsset.ERROR("WaitForCompletion fatal error");
#endif
        }
    }
}