using Saro.Net;

namespace Saro.XAsset
{
    public sealed class WebBundleHandle : BundleHandle
    {
        private IDownloadAgent m_DownloadAgent;

        public DownloadInfo Info { get; set; }

        public int RetryCount { get; set; } = XAssetConfig.s_MaxDownloadRetryCount;

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

                if (m_DownloadAgent == null || LoadState == ELoadState.Loaded)
                    return true;

                if (m_DownloadAgent.IsDone)
                {
                    bool finish = false;

                    if (m_DownloadAgent.Status == EDownloadStatus.Success)
                    {
                        // 下载成功，调用基类方法加载bundle
                        base.Load();

                        finish = true;
                    }
                    else
                    {
                        if (--RetryCount >= 0)
                        {
                            StartDownload();
                        }
                        else
                        {
                            XAssetManager.Current.OnLoadRemoteAssetError?.Invoke(AssetUrl);

                            finish = true;
                        }
                    }

                    if (finish)
                    {
                        LoadState = ELoadState.Loaded;

                        XAssetManager.Current.OnLoadRemoteAsset?.Invoke(Info.SavePath, true);
                    }
                }

                return m_DownloadAgent.IsDone;
            }
        }

        public override float Progress
        {
            get { return m_DownloadAgent != null ? m_DownloadAgent.Progress : 0f; }
        }

        internal override void Load()
        {
            StartDownload();

            XAssetManager.Current.OnLoadRemoteAsset?.Invoke(Info.DownloadUrl, false);

            LoadState = ELoadState.LoadAssetBundle;
        }

        internal override void Unload()
        {
            LoadState = ELoadState.Unload;
            base.Unload();
        }

        private void StartDownload()
        {
            m_DownloadAgent = Downloader.DownloadAsync(Info);
        }
    }
}