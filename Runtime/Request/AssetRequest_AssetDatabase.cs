using System;
using System.IO;
using UnityEngine;

using UObject = UnityEngine.Object;

namespace Saro.MoonAsset
{
    // 编辑器下模拟 async，提前暴露问题

    public sealed class AssetRequest_AssetDatabase : Request
    {
        private float m_LoadTime;
        public bool IsComponent => typeof(Component).IsAssignableFrom(AssetType);

        public override bool IsDone
        {
            get
            {
#if UNITY_EDITOR
                if (LoadState == ELoadState.Loaded) return true;

                if (m_IsWaitForCompletion)
                {
                    LoadAsset();
                    return true;
                }
                else
                {
                    var done = Time.realtimeSinceStartup > m_LoadTime;
                    if (done)
                    {
                        LoadAsset();
                    }
                    return done;
                }
#else
                return true;
#endif
            }
        }

        private bool m_IsWaitForCompletion;

        internal override void Load()
        {
            const float delay = 0.01f; // 如果觉得卡，就调小一点
            m_LoadTime = Time.realtimeSinceStartup + delay;

#if !UNITY_EDITOR
            LogError();
#endif
        }

        private void LoadAsset()
        {
#if UNITY_EDITOR
            if (IsComponent)
            {
                throw new InvalidOperationException($"[MoonAsset] not support typeof(Component) {AssetType}, use GameObject instead. url: {Path.GetFileName(AssetUrl)}");
            }

            Asset = UnityEditor.AssetDatabase.LoadAssetAtPath(AssetUrl, AssetType);
#endif
            if (Asset == null) LogError();

            LoadState = ELoadState.Loaded;
        }

        internal override void Unload(bool unloadAllObjects = true)
        {
            m_IsWaitForCompletion = false;

#if UNITY_EDITOR
            m_LoadTime = 0f;
            Asset = null;
#endif
        }

        public override void WaitForCompletion()
        {
            m_IsWaitForCompletion = true;

            InvokeWaitForCompletion();

#if DEBUG
            if (!IsDone)
                MoonAsset.ERROR("WaitForCompletion fatal error");
#endif
        }

        void LogError()
        {
            MoonAsset.ERROR($"{nameof(AssetRequest_AssetDatabase)} load asset failed. {nameof(AssetUrl)}: {AssetUrl} {nameof(AssetType)}: {AssetType}");
        }
    }
}
