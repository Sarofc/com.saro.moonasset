using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace Saro.MoonAsset
{
    public abstract class BundleRequest : Request
    {
        public List<BundleRequest> Dependencies { get; private set; } = new();

        public virtual AssetBundle Bundle
        {
            get => Asset as AssetBundle;
            internal set => Asset = value;
        }
    }
}