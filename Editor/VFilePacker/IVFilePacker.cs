using System;
using System.IO;

namespace Saro.MoonAsset.Build
{
    // TODO vfile 搞一个 buildlog

    public interface IVFilePacker
    {
        bool PackVFile(string dstFolder);

        static void PackVFiles()
        {
            var dstFolder = MoonAssetConfig.k_Editor_ResRawFolderPath;
            if (!Directory.Exists(dstFolder))
                Directory.CreateDirectory(dstFolder);

            var types = Saro.Utility.TypeUtility.GetSubClassTypesAllAssemblies(typeof(IVFilePacker));
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
