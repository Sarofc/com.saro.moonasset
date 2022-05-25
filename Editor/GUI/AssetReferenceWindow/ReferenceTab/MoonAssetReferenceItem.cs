using Saro.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace Saro.MoonAsset
{
    public class MoonAssetReferenceItem : TreeViewItem
    {
        public List<IAssetHandle> handles;

        public MoonAssetReferenceItem(List<IAssetHandle> handles)
        {
            this.handles = handles;
        }

        public int GetRefCount()
        {
            var refCount = 0;
            for (int i = 0; i < handles.Count; i++)
            {
                var handle = handles[i];
                refCount += handle.RefCount;
            }
            return refCount;
        }

        internal string GetAssetURL()
        {
            var handle = handles.First();
            if (handle == null) return "";

            if (handle.AssetType == null) // AssetType == null 代表为 AssetBundle
                return Path.GetFileName(handle.AssetUrl);

            return handle.AssetUrl;
        }

        internal string GetAssetType()
        {
            var handle = handles.First();
            if (handle == null) return "";

            if (handle.AssetType == null) // AssetType == null 代表为 AssetBundle
                return "";

            return handle.AssetType.Name;
        }
    }
}
