using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Saro.Core;

namespace Saro.MoonAsset
{
    public partial class MoonAsset
    {
        public IAssetTable AssetTable { get; private set; }

        public void LoadAssetTable(IAssetTableProvider provider)
        {
            AssetTable = provider.GetAssetTable(this);
        }

        public async UniTask LoadAssetTableAsync(IAssetTableProvider provider)
        {
            AssetTable = await provider.GetAssetTableAsync(this);
        }

        public IAssetHandle LoadAsset(int assetID, Type type)
        {
            Log.Assert(AssetTable != null, $"MUST LoadAssetTable/LoadAssetTableAsync first");

            var assetPath = AssetTable.GetAssetPath(assetID);
            return LoadAsset(assetPath, type);
        }

        public IAssetHandle LoadAssetAsync(int assetID, Type type)
        {
            Log.Assert(AssetTable != null, $"MUST LoadAssetTable/LoadAssetTableAsync first");

            var assetPath = AssetTable.GetAssetPath(assetID);
            return LoadAssetAsync(assetPath, type);
        }
    }
}