using System;
using System.Threading.Tasks;
using Saro.Core;

namespace Saro.MoonAsset
{
    partial class MoonAsset
    {
        public static async Task<MoonAsset> RegisterAsync(bool deguggerGUI = false)
        {
            MoonAsset moonAsset = new();
            moonAsset.AddDefaultLocators();
            await moonAsset.InitializeAsync();
            Main.Register<IAssetManager>(moonAsset);

            if (deguggerGUI)
                Main.Instance.gameObject.AddComponent<MoonAssetDebuggerGUI>();

            INFO("Initialize Done MoonAsset");

            return moonAsset;
        }
    }
}
