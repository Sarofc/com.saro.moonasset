using Cysharp.Threading.Tasks;
using Saro.Net;
using Saro.UI;
using Saro.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Saro.MoonAsset.Update
{
    /*
     * 参考下 addressable 吧，这块逻辑有点杂乱
     * 
     * TODO
     *
     * 1.还是要校验文件完整性，使用md5
     * 2.custom bundle压缩
     * 3. 断点续传 逻辑 有误，appendhash后，可以全部用断点续传
     *
     * 0.下载到临时目录的必要性？
     * 1.流程Async化，需要支持 资源开始，暂停，取消 接口
     * 2.提供UI接口
     * 3.弱网环境测试
     *
     */
    public sealed class AssetUpdaterManager : IService, IUpdater, INetworkMonitorListener
    {
        private enum EStep
        {
            Wait,
            RequestLocalVersionManifest,
            RequestRemoteVersionManifest,
            DiffAssetInfos,
            DownloadAssets,
            UpdateSuccess,
            UpdateFailed,
        }

        public IUpdater Listener { get; set; }
        public bool IsComplete => m_Step == EStep.UpdateSuccess;

        private EStep m_Step;
        private NetworkMonitorComponent m_NetworkMonitor;

        private bool m_NetReachabilityChanged;

        /// <summary>
        /// remote和local版本号是否一致
        /// </summary>
        private bool m_ManifestVersionSame = true;

        void IService.Awake()
        {
            Downloader.s_GlobalCompleted += VerifyCallback;

            m_NetworkMonitor = Main.Register<NetworkMonitorComponent>();
            m_NetworkMonitor.Listener = this;

            m_Step = EStep.Wait;

            Main.onApplicationFocus -= OnApplicationFocus;
            Main.onApplicationFocus += OnApplicationFocus;
        }

        public async UniTask StartUpdate()
        {
            var mode = MoonAsset.s_Mode;

            if (mode != MoonAsset.EMode.Runtime)
            {
                INFO($"Mode = {mode}，不热更。");
                m_Step = EStep.UpdateSuccess;
                return;
            }

            INFO("Start Update...");

            m_Step = EStep.Wait;

            // TODO 先申请 版本号小文件 记录有 appVersion resVersion

            //var localManifest = LoadLocalManifestAsync();
            var localManifest = MoonAsset.Current.Manifest;

            // TODO 再比较 appVersion 和 resVersion，需要时再申请 RemoteManifest

            // TODO
            //m_ManifestVersionNotSame = false;

            var remoteManifest = await RequestRemoteManifestAsync();
            if (remoteManifest == null)
            {
                ERROR("Remote Manifest is null");
                m_Step = EStep.UpdateFailed;
                Retry();
                return;
            }

            if (localManifest != null && remoteManifest != null)
            {
                m_ManifestVersionSame = localManifest.resVersion == remoteManifest.resVersion;
            }
            else
            {
                m_ManifestVersionSame = true;
            }

            m_DiffAssets = GetDiffAssetInfos(localManifest, remoteManifest);

            if (m_DiffAssets.Count <= 0)
            {
                INFO("Nothing to Download...");
                m_Step = EStep.UpdateSuccess;
                return;
            }

            // 这里优化下，弄成接口吧
            var downloadOp = await RequestDownloadOperationFunc(m_DiffAssets);

            if (downloadOp)
            {
                var success = await DownloadAsync(m_DiffAssets);

                if (success)
                {
                    if (OverrideManifest(localManifest, remoteManifest))
                    {
                        m_Step = EStep.UpdateSuccess;
                    }
                    else
                    {
                        m_Step = EStep.UpdateFailed;
                    }
                }
                else
                {
                    m_Step = EStep.UpdateFailed;
                }
            }
            else
            {
                // TODO 应该是直接退游戏了
                ERROR("User Cancel Download...");
                Main.Quit();
            }

            if (m_Step == EStep.UpdateFailed)
            {
                Retry();
            }
            else if (m_Step == EStep.UpdateSuccess)
            {
                INFO("Update Finish...");

                MoonAsset.Current.ClearAssetReference(true);
            }
        }

        private void Retry()
        {
            //ERROR("!!! UpdateFailed, Show MessageBox");

            // TODO 包装起来，不应该引用到UIComponent
            var info = new Saro.UI.AlertDialogInfo
            {
                title = "下载失败",
                content = $"请检查网络后，重新下载",
                leftText = "退出",
                rightText = "下载",
                clickHandler = (state) =>
               {
                   if (state == 0)
                       Main.Quit();
                   else if (state == 1)
                   {
                       INFO("retry StartUpdate");
                       StartUpdate().Forget();
                   }
               }
            };

            Saro.UI.UIManager.Current.Queue(EDefaultUI.UIAlertDialog, 0, info, EUILayer.Top);
        }

        private Manifest LoadLocalManifestAsync()
        {
            m_Step = EStep.RequestLocalVersionManifest;

            var localManifest = MoonAsset.Current.LoadLocalManifest(MoonAssetConfig.k_ManifestAsset);

            return localManifest;
        }

        private async UniTask<Manifest> RequestRemoteManifestAsync()
        {
            m_Step = EStep.RequestRemoteVersionManifest;

            var tmpManifestPath = MoonAssetConfig.k_TmpManifestAssetPath;
            var request = UnityWebRequest.Get(MoonAssetConfig.GetRemoteAssetURL(MoonAssetConfig.k_ManifestAsset));
            request.downloadHandler = new DownloadHandlerFile(tmpManifestPath)
            {
                removeFileOnAbort = true
            };

            UnityWebRequest _request = null;
            try
            {
                _request = await request.SendWebRequest();
            }
            catch (Exception e)
            {
                ERROR(e.ToString());
                return null;
            }

#if UNITY_2020_1_OR_NEWER
            if (_request.result != UnityWebRequest.Result.Success)
#else
            if(_request.isDone && !_request.isNetworkError)
#endif
            {
                ERROR(_request.error);
            }
            else
            {
                return Manifest.Create(tmpManifestPath);
            }

            return null;
        }

        private List<DownloadInfo> GetDiffAssetInfos(Manifest local, Manifest remote)
        {
            m_Step = EStep.DiffAssetInfos;

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var diffAssets = Manifest.Diff(local, remote);
            sw.Stop();

            INFO($"diff = [local:{local?.resVersion}] [remote:{remote?.resVersion}] UseRESUME: {m_ManifestVersionSame} CostTimes: {sw.ElapsedMilliseconds} ms \n{ string.Join("\n", diffAssets)}");

            var downloadInfos = new List<DownloadInfo>(diffAssets.Count());

            foreach (var asset in diffAssets)
            {
                var info = new DownloadInfo
                {
                    DownloadUrl = MoonAssetConfig.GetRemoteAssetURL(asset.Name),
                    SavePath = MoonAssetConfig.GetLocalAssetURL(asset.Name),
                    Size = asset.Size,
                    Hash = asset.Hash,
                    //UseRESUME = m_ManifestVersionSame, // 版本号一致时，才使用断点续传。！！！！此逻辑有误
                    UseRESUME = true, // append hash后，可以直接使用断点续传，最坏的情况，就是下完校验失败，重新下载而已
                };
                downloadInfos.Add(info);
            }

            return downloadInfos;
        }

        public Func<List<DownloadInfo>, UniTask<bool>> RequestDownloadOperationFunc = (infos) =>
        {
            var task = new UniTask<bool>(true);
            return task;
        };

        private async UniTask<bool> DownloadAsync(IList<DownloadInfo> infos)
        {
            m_Step = EStep.DownloadAssets;

            if (infos == null || infos.Count == 0)
            {
                INFO("Nothing to download");
                return true;
            }

            INFO($"download list:\n{string.Join("\n", infos)}");

            var tasks = new List<UniTask>(infos.Count);

            for (int i = 0; i < infos.Count; i++)
            {
                var info = infos[i];

                if (m_HasDownloadFile.Contains(info.DownloadUrl)) continue;

                var httpDownload = Downloader.DownloadAsync(info);

                var task = httpDownload.ToUniTask();
                tasks.Add(task);
            }

            // 中途退出游戏，此状态也算完成了，实际是错误的
            await UniTask.WhenAll(tasks);

            // 校验文件清单
            var success = VerifyAssetList();

            return success;
        }

        private List<DownloadInfo> m_DiffAssets;

        //  已下载文件记录，貌似只用丢在内存了就够了
        //  重启游戏后，由于manifest没变，所以，依然要下载这些资源，只不过是瞬间完成而已
        private HashSet<string> m_HasDownloadFile = new HashSet<string>();

        private bool VerifyAssetList()
        {
            foreach (var asset in m_DiffAssets)
            {
                if (!m_HasDownloadFile.Contains(asset.DownloadUrl))
                {
                    ERROR($"missing asset: {asset.DownloadUrl}");
                    return false;
                }
            }

            return true;
        }

        //private List<UniTask<bool>> m_CompressTasks = new List<UniTask<bool>>();
        private void VerifyCallback(IDownloadAgent agent)
        {
            //ERROR($"!!! VerifyCallback: {obj.Info.DownloadUrl}");

            if (agent.Status == EDownloadStatus.Success)
            {
                bool errorFile = false;

                if (!string.IsNullOrEmpty(agent.Info.Hash))
                {
                    // 校验hash
                    using (var fs = new FileStream(agent.Info.SavePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var hash = HashUtility.GetMd5HexHash(fs);
                        if (!HashUtility.VerifyMd5HexHash(agent.Info.Hash, hash))
                        {
                            ERROR($"Download {agent.Info.DownloadUrl} with error md5 missmatch [{agent.Info.Hash}] [{hash}]");

                            errorFile = true;
                        }
                        else
                        {
                            bool ret = m_HasDownloadFile.Add(agent.Info.DownloadUrl);
                            if (!ret)
                            {
                                ERROR($"Duplicate download {agent.Info.DownloadUrl}");
                            }
                            else
                            {
                                // TODO md5 校验成功，开始解压
                                // io操作，线程不一定好，改为使用 异步io
                                //if (agent.Info.UserData is AssetUpdateData _data)
                                //{
                                //    if (_data.compress)
                                //    {
                                //        var task = UnCompressAsync();

                                //        m_CompressTasks.Add(task);
                                //    }
                                //}
                            }
                        }
                    }
                }

                if (errorFile)
                {
                    // TODO 下载器在线程里跑，文件流有可能没关闭，这里要测试下有没有问题
                    File.Delete(agent.Info.SavePath);
                }
            }
            else
            {
                ERROR($"Unable to download {agent.Info.DownloadUrl} with error {agent.Error}");
            }
        }

        private bool OverrideManifest(Manifest local, Manifest remote)
        {
            try
            {
                if (local == null)
                {
                    local = ScriptableObject.CreateInstance<Manifest>();
                }

                local.Override(remote, MoonAssetConfig.k_ManifestAssetPath);

                return true;
            }
            catch (Exception e)
            {
                ERROR("OverrideLocalManifestUseTmp Error(): " + e);
                return false;
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (m_NetReachabilityChanged || m_Step == EStep.Wait) return;

            if (hasFocus)
            {
                if (m_Step == EStep.DownloadAssets)
                {
                    Downloader.ResumeAllDownloads();
                }
            }
            else
            {
                if (m_Step == EStep.DownloadAssets)
                {
                    Downloader.PauseAllDownloads();
                }
            }
        }

        #region Interface

        public void OnClear()
        {
            OnMessage("清理更新");
            OnProgress(0);

            m_Step = EStep.Wait;
            m_NetReachabilityChanged = false;

            //MoonAsset.Current.ClearAssetReference();

            if (Listener != null)
            {
                Listener.OnClear();
            }

            if (Directory.Exists(MoonAssetConfig.k_DlcPath))
            {
                Directory.Delete(MoonAssetConfig.k_DlcPath, true);
            }
        }

        public void OnMessage(string msg)
        {
            if (Listener != null)
            {
                Listener.OnMessage(msg);
            }
        }

        public void OnProgress(float progress)
        {
            if (Listener != null)
            {
                Listener.OnProgress(progress);
            }
        }

        public void OnStart()
        {
            if (Listener != null)
            {
                Listener.OnStart();
            }
        }

        public void OnVersion(string ver)
        {
            if (Listener != null)
            {
                Listener.OnVersion(ver);
            }
        }

        void INetworkMonitorListener.OnReachablityChanged(NetworkReachability reachability)
        {

        }

        #endregion


        [System.Diagnostics.Conditional(Log.k_ScriptDefineSymbol)]
        private void INFO(string msg)
        {
            Log.INFO("AssetUpdate", msg);
        }


        [System.Diagnostics.Conditional(Log.k_ScriptDefineSymbol)]
        private void WARN(string msg)
        {
            Log.WARN("AssetUpdate", msg);
        }

        private void ERROR(string msg)
        {
            Log.ERROR("AssetUpdate", msg);
        }

        void IService.Update()
        {
        }

        void IService.Dispose()
        {
            Downloader.s_GlobalCompleted -= VerifyCallback;

            Main.onApplicationFocus -= OnApplicationFocus;
        }
    }
}