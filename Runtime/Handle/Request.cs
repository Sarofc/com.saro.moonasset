using Cysharp.Threading.Tasks;
using Saro.Core;
using System;
using System.IO;
using UnityEngine;

namespace Saro.MoonAsset
{
    using UObject = UnityEngine.Object;

    public enum ELoadState
    {
        Init,
        Downloading,
        LoadAssetBundle,
        LoadAsset,
        Loaded,
        Unload,
    }

    public abstract class Request : RC, IAssetHandle
    {
        public Type AssetType { get; set; }
        public string AssetUrl { get; set; }

        /// <summary>
        /// 资源加载状态
        /// </summary>
        public ELoadState LoadState { get; protected set; }

        public Request()
        {
            Asset = null;
            LoadState = ELoadState.Init;
        }

        public virtual bool IsDone => true;

        public virtual bool IsError => !string.IsNullOrEmpty(Error);

        public virtual float Progress => 1f;

        public virtual string Error { get; protected set; }

        public string Text { get; protected set; }

        public byte[] Bytes { get; protected set; }

        public UObject Asset { get; protected set; }

        internal MoonAsset MoonAsset { get; set; }

        internal abstract void Load();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unloadAllObjects">only for AssetBundle</param>
        internal abstract void Unload(bool unloadAllObjects = true);

        public abstract void WaitForCompletion();

        internal void InvokeWaitForCompletion()
        {
            if (MoonAssetConfig.PlatformUsesMultiThreading(Application.platform))
            {
                const float max_timeout = 10f; // 同步加载，默认超时时间
                float timeout = max_timeout + Time.realtimeSinceStartup;
                //var start = Time.realtimeSinceStartup;
                while (!IsDone)
                {
                    if (timeout <= Time.realtimeSinceStartup)
                    {
                        MoonAsset.ERROR($"sync load timeout: {max_timeout}s, try async load instead. {AssetUrl}");
                        break;
                    }
                }
                //MoonAsset.INFO($"sync load cost: {Time.realtimeSinceStartup - start}");
            }
            else
            {
                throw new PlatformNotSupportedException($"[MoonAsset] {Application.platform} not support sync load");
            }
        }

        internal bool Update()
        {
            if (!IsDone)
                return true;

            InvokeCompleted();

            if (IsError)
                MoonAsset.ERROR(Error);

            return false;
        }

        void InvokeCompleted()
        {
            if (Completed != null)
            {
                try
                {
                    Completed(this);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                finally
                {
                    Completed = null;
                }
            }
        }

        public Action<IAssetHandle> Completed { get; set; }

        #region IEnumerator Impl

        public bool MoveNext() => !IsDone;

        public void Reset() { }

        public object Current => null;

        #endregion

        public override string ToString()
        {
            return $"{this.GetType().Name}. {Path.GetFileName(AssetUrl)}";
        }
    }
}