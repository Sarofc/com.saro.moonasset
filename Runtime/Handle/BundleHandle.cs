using System.Collections.Generic;
using UnityEngine;

namespace Saro.MoonAsset
{
    public class BundleHandle : AssetHandle
    {
        public List<BundleHandle> Dependencies { get; private set; } = new();

        public virtual AssetBundle Bundle
        {
            get => Asset as AssetBundle;
            internal set => Asset = value;
        }

        internal override void Load()
        {
            Asset = AssetBundle.LoadFromFile(AssetUrl);

            if (Bundle == null)
                Error = "LoadFromFile failed. AsserUrl: " + AssetUrl;
        }

        internal override void Unload(bool unloadAllObjects = true)
        {
            if (Bundle == null)
                return;
            Bundle.Unload(unloadAllObjects);
            Bundle = null;
        }
    }
}