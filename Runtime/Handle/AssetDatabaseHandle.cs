﻿namespace Saro.MoonAsset
{
    public sealed class AssetDatabaseHandle : AssetHandle
    {
        internal override void Load()
        {
#if UNITY_EDITOR
            Asset = UnityEditor.AssetDatabase.LoadAssetAtPath(AssetUrl, AssetType);
#endif

            if (Asset == null)
                MoonAsset.ERROR($"load asset failed. {nameof(AssetUrl)}: {AssetUrl} {nameof(AssetType)}: {AssetType}");
        }

        internal override void Unload(bool unloadAllObjects = true)
        {
#if UNITY_EDITOR
            Asset = null;
#endif
        }
    }
}
