using Saro.Core;
using System;
using UnityEngine;

namespace Saro.XAsset
{
    using Object = UnityEngine.Object;

    public enum ELoadState
    {
        Init,
        LoadAssetBundle,
        LoadAsset,
        Loaded,
        Unload,
    }

    public abstract class AssetHandle : RC, IAssetHandle
    {
        public Type AssetType { get; set; }
        public string AssetUrl { get; set; }

        /// <summary>
        /// 资源加载状态
        /// </summary>
        public ELoadState LoadState { get; protected set; }

        public AssetHandle()
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

        public Object Asset { get; protected set; }

        public T GetAsset<T>() where T : Object
        {
            return Asset as T;
        }

        internal abstract void Load();

        internal abstract void Unload();

        internal bool Update()
        {
            if (!IsDone)
                return true;
            if (Completed == null)
                return false;
            try
            {
                Completed.Invoke(this);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            Completed = null;
            return false;
        }

        public Action<IAssetHandle> Completed { get; set; }

        #region DelayUnload

        // TODO 开个分支再弄！

        private float m_UnloadTime = -1f;

        internal void MarkUnload(bool immediate)
        {
            if (XAssetManager.Current.Policy.AutoUnloadAsset)
            {
                m_UnloadTime = immediate ? 0.1f : Time.unscaledTime + XAssetManager.Current.Policy.UnusedAssetUnloadDelay;
            }
        }

        internal void UnMarkUnload()
        {
            if (XAssetManager.Current.Policy.AutoUnloadAsset)
            {
                m_UnloadTime = -1f;
            }
        }

        internal bool IsMarkUnload()
        {
            if (!XAssetManager.Current.Policy.AutoUnloadAsset)
                return false;

            return m_UnloadTime > 0f;
        }

        internal bool IsReadyUnload()
        {
            if (!XAssetManager.Current.Policy.AutoUnloadAsset)
                return true;

            return m_UnloadTime <= Time.unscaledTime;
        }

        #endregion

        #region IEnumerator Impl

        public bool MoveNext()
        {
            return !IsDone;
        }

        public void Reset()
        {
        }

        public object Current => null;

        #endregion
    }
}