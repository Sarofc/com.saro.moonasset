using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Saro.XAsset
{
    using Object = UnityEngine.Object;

    [System.Obsolete("不再提供直接下载远端单个资源，自行使用http等方法下载", true)]
    public sealed class WebAssetHandle : AssetHandle
    {
        private UnityWebRequest m_UnityWebRequest;

        public override bool IsDone
        {
            get
            {
                if (LoadState == ELoadState.Init)
                    return false;
                if (LoadState == ELoadState.Loaded)
                    return true;

                if (LoadState == ELoadState.LoadAsset)
                {
                    if (m_UnityWebRequest == null || !string.IsNullOrEmpty(m_UnityWebRequest.error))
                        return true;

                    if (m_UnityWebRequest.isDone)
                    {
                        if (AssetType == typeof(Texture2D))
                        {
                            Asset = DownloadHandlerTexture.GetContent(m_UnityWebRequest);
                        }
                        else if (AssetType == typeof(TextAsset))
                        {
                            Text = m_UnityWebRequest.downloadHandler.text;
                        }
                        else if (AssetType == typeof(AudioClip))
                        {
                            Asset = DownloadHandlerAudioClip.GetContent(m_UnityWebRequest);
                        }
                        else
                        {
                            Bytes = m_UnityWebRequest.downloadHandler.data;
                        }

                        LoadState = ELoadState.Loaded;
                        return true;
                    }

                    return false;
                }

                return true;
            }
        }

        public override string Error
        {
            get { return m_UnityWebRequest.error; }
        }

        public override float Progress
        {
            get { return m_UnityWebRequest.downloadProgress; }
        }

        internal override void Load()
        {
            if (AssetType == typeof(AudioClip))
            {
                var audioType = AudioType.UNKNOWN;
                var ext = Path.GetExtension(AssetUrl);

                switch (ext)
                {
                    case ".mp3":
                    case ".mp2":
                        audioType = AudioType.MPEG;
                        break;
                    case ".ogg":
                        audioType = AudioType.OGGVORBIS;
                        break;
                    case ".vag":
                        audioType = AudioType.MPEG;
                        break;
                    case ".wav":
                        audioType = AudioType.WAV;
                        break;
                }

                if (audioType == AudioType.UNKNOWN)
                {
                    Log.ERROR($"AudioType {ext} is not supoort");
                    LoadState = ELoadState.Loaded;
                }

                m_UnityWebRequest = UnityWebRequestMultimedia.GetAudioClip(AssetUrl, audioType);
            }
            else if (AssetType == typeof(Texture2D))
            {
                m_UnityWebRequest = UnityWebRequestTexture.GetTexture(AssetUrl);
            }
            else
            {
                m_UnityWebRequest = new UnityWebRequest(AssetUrl);
                m_UnityWebRequest.downloadHandler = new DownloadHandlerBuffer();
            }

            m_UnityWebRequest.SendWebRequest();
            LoadState = ELoadState.LoadAsset;
        }

        internal override void Unload()
        {
            if (Asset != null)
            {
                Object.Destroy(Asset);
                Asset = null;
            }

            if (m_UnityWebRequest != null)
                m_UnityWebRequest.Dispose();

            Bytes = null;
            Text = null;
        }
    }
}