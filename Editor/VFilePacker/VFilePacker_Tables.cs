using System.IO;
using Saro.MoonAsset.Build;
using UnityEditor;
using UnityEngine;
using Saro.IO;
using System;

namespace Saro.MoonAsset.Build
{
    internal sealed class VFilePacker_Tables : IVFilePacker
    {
        static string vfileName = "tables";

        bool IVFilePacker.PackVFile(string dstFolder)
        {
            var dstVFilePath = dstFolder + "/" + vfileName;

            BuildVFile(dstVFilePath);

            return true;
        }

        private static void BuildVFile(string vfilePath)
        {
            try
            {
                if (File.Exists(vfilePath))
                    File.Delete(vfilePath);

                var tablePath = "GameTools/tables/data/config";
                var files = Directory.GetFiles(tablePath, "*", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    using (var vfile = VFileSystem.Open(vfilePath, FileMode.CreateNew, FileAccess.ReadWrite, files.Length, files.Length))
                    {
                        for (int i = 0; i < files.Length; i++)
                        {
                            string file = files[i];

                            EditorUtility.DisplayCancelableProgressBar("打包数据表", $"{file}", (i + 1f) / files.Length);

                            var result = vfile.WriteFile($"{Path.GetFileName(file)}", file);
                            if (!result)
                            {
                                Debug.LogError($"[VFilePacker_Tables] 打包失败： {file} ");
                                continue;
                            }
                        }

                        Debug.LogError("[VFilePacker_Tables]\n" + string.Join("\n", vfile.GetAllFileInfos()));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[VFilePacker_Tables] 打包数据表 error:" + e);
                return;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
