using System;

namespace Saro.MoonAsset.Build
{
    public interface IVFilePacker
    {
        bool PackVFile(string dstFolder);

        static void PackVFiles()
        {
            var dstFolder = MoonAssetConfig.k_Editor_ResRawFolderPath;
            var types = Saro.Utility.ReflectionUtility.GetSubClassTypesAllAssemblies(typeof(IVFilePacker));
            foreach (var type in types)
            {
                var packer = Activator.CreateInstance(type) as IVFilePacker;
                var result = packer.PackVFile(dstFolder);
                if (result)
                {
                }
                else
                {
                    MoonAsset.ERROR($"{type} pack vfile failed");
                }
            }
        }
    }
}
