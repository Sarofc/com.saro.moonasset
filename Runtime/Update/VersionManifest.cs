//using Saro.IO;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using UnityEngine;

//namespace Saro.XAsset.Update
//{
//    // TODO 参考 coscos 热更新 重构下 http://docs.cocos.com/creator/manual/zh/advanced-topics/assets-manager.html
//    // VersionFile 只含有版本
//    // Manifest 含有 全部信息
//    [System.Obsolete("去掉 直接使用 Manifest", true)]
//    public sealed class VersionManifest
//    {
//        /// <summary>
//        /// 游戏版本
//        /// </summary>
//        public Version appVersion = new Version(Application.version);
//        /// <summary>
//        /// 资源版本
//        /// </summary>
//        public int resVersion;
//        /// <summary>
//        /// [可选] 远程版本文件的路径，用来判断服务器端是否有新版本的资源
//        /// </summary>
//        public string remoteVersionUrl = "";
//        /// <summary>
//        /// 远程资源 Manifest 文件的路径，包含版本信息以及所有资源信息
//        /// </summary>
//        public string remoteManifestUrl = "";
//        /// <summary>
//        /// 资源列表
//        /// </summary>
//        public Dictionary<string, AssetInfo> assets;


//        [System.Flags]
//        public enum EVerifyBy : byte
//        {
//            None = 0,
//            Crc32 = 1,
//            Md5 = 2,
//        }

//        public static readonly EVerifyBy s_VerifyBy = EVerifyBy.Md5;

//        public static System.Version LoadVersionOnly(BinaryReader reader)
//        {
//            return new System.Version(reader.ReadString());
//        }

//        public static System.Version LoadVersionOnly(string versionListPath)
//        {
//            if (!File.Exists(versionListPath))
//                return null;

//            FileStream fs = null;
//            BinaryReader br = null;

//            try
//            {
//                fs = File.OpenRead(versionListPath);
//                br = new BinaryReader(fs);
//                return new System.Version(br.ReadString());
//            }
//            catch (Exception e)
//            {
//                UnityEngine.Debug.LogException(e);
//            }
//            finally
//            {
//                if (fs != null) fs.Close();
//                if (br != null) br.Close();
//            }

//            return null;
//        }

//        public static VersionManifest LoadVersionManifest(string versionListPath)
//        {
//            if (!File.Exists(versionListPath))
//            {
//                return null;
//            }

//            var retList = new VersionManifest();

//            try
//            {
//                using (var fs = File.OpenRead(versionListPath))
//                {
//                    using (var br = new BinaryReader(fs))
//                    {
//                        retList.Deserialize(br);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                Log.INFO(nameof(VersionManifest), e.ToString());
//            }

//            return retList;
//        }

//        //        [System.Diagnostics.Conditional("UNITY_EDITOR")]
//        //        public static void BuildVersionList(string outputFolder, string datFolder, string[] bundles, Version version)
//        //        {
//        //#if UNITY_EDITOR

//        //            if (!Directory.Exists(datFolder)) Directory.CreateDirectory(datFolder);

//        //            var versionAssetInfos = new Dictionary<string, AssetInfo>(bundles.Length);
//        //            var versionList = new VersionManifest
//        //            {
//        //                resVersion = version,
//        //                assets = versionAssetInfos
//        //            };

//        //            // TODO 超过4g时，分文件

//        //            var datFullPath = datFolder + "/" + k_DatFileName;

//        //            using (var vfsStream = new CommonVFileSystemStream(datFullPath, EVFileSystemAccess.Write, true))
//        //            {
//        //                using (var vfs = VFileSystem.Create(datFullPath, EVFileSystemAccess.Write, vfsStream, 1024, 1024 * 32))
//        //                {
//        //                    foreach (var bundle in bundles)
//        //                    {
//        //                        var bundlePath = outputFolder + "/" + bundle;
//        //                        using (var fs = File.OpenRead(bundlePath))
//        //                        {
//        //                            if (vfs.WriteFile(bundle, fs))
//        //                            {
//        //                                fs.Seek(-fs.Length, SeekOrigin.End);

//        //                                var fileInfo = vfs.GetFileInfo(bundle);
//        //                                var assetInfo = new AssetInfo
//        //                                {
//        //                                    key = fileInfo.Name,
//        //                                    md5 = GetHashUseEVerifyBy(fs),
//        //                                    length = fileInfo.Length,
//        //                                    offset = fileInfo.Offset,
//        //                                };
//        //                                versionAssetInfos.Add(assetInfo.key, assetInfo);
//        //                            }
//        //                            else
//        //                            {
//        //                                UnityEngine.Debug.LogError($"[XAsset]. write vfs failed. vfsName: {k_DatFileName} fileName: {bundle}");
//        //                                break;
//        //                            }
//        //                        }
//        //                    }
//        //                }
//        //            }


//        //            var versionFilePath = datFolder + "/" + k_VersionManifestFileName;

//        //            using (var fs = File.OpenWrite(versionFilePath))
//        //            {
//        //                using (var bw = new BinaryWriter(fs))
//        //                {
//        //                    versionList.Serialize(bw);
//        //                }
//        //            }

//        //            File.WriteAllText(versionFilePath + ".dump.txt", versionList.Dump());

//        //            // test
//        //            //using (var vfsStream = new CommonVFileSystemStream(datFullPath, EVFileSystemAccess.Read, false))
//        //            //{
//        //            //    using (var vfs = VFileSystem.Load(datFullPath, EVFileSystemAccess.Read, vfsStream))
//        //            //    {
//        //            //        var fileInfos = vfs.GetAllFileInfos();
//        //            //        foreach (var fileInfo in fileInfos)
//        //            //        {
//        //            //            var hash = Utility.HashUtility.GetCRC32Hash(vfs.ReadFile(fileInfo.Name));
//        //            //            UnityEngine.Debug.LogError(fileInfo.Name + ": " + hash);
//        //            //        }
//        //            //    }
//        //            //}

//        //#endif
//        //        }

//        [System.Diagnostics.Conditional("UNITY_EDITOR")]
//        public static void BuildVersionManifest(string outputFolder, string[] bundles, int version)
//        {
//            var ver = new VersionManifest();
//            ver.resVersion = version;
//            ver.assets = new Dictionary<string, AssetInfo>(bundles.Length);
//            foreach (var bundle in bundles)
//            {
//                var bundlePath = outputFolder + "/" + bundle;
//                using (var fs = File.OpenRead(bundlePath))
//                {
//                    var assetInfo = new AssetInfo
//                    {
//                        key = bundle,
//                        md5 = GetHashUseEVerify(fs),
//                        size = fs.Length,
//                        offset = 0L,
//                    };
//                    ver.assets.Add(assetInfo.key, assetInfo);
//                }
//            }

//            var verpath = Path.Combine(outputFolder, XAssetPath.k_VersionManifestFileName);
//            using (var fs = new FileStream(verpath, FileMode.OpenOrCreate))
//            {
//                fs.Seek(0L, SeekOrigin.Begin);
//                using (var bw = new BinaryWriter(fs))
//                {
//                    ver.Serialize(bw);
//                }
//            }

//            File.WriteAllText(verpath + ".dump.txt", ver.Dump());
//        }

//        public static void AddExtraAssetsToVersionManifest(string versionListPath, string[] assetPaths)
//        {
//            var ver = LoadVersionManifest(versionListPath);

//            foreach (var assetPath in assetPaths)
//            {
//                using (var fs = File.OpenRead(assetPath))
//                {
//                    var assetInfo = new AssetInfo
//                    {
//                        key = Path.GetFileName(assetPath),
//                        md5 = GetHashUseEVerify(fs),
//                        size = fs.Length,
//                        offset = 0L,
//                    };
//                    ver.assets.Add(assetInfo.key, assetInfo);
//                }
//            }

//            using (var fs = new FileStream(versionListPath, FileMode.OpenOrCreate))
//            {
//                fs.Seek(0L, SeekOrigin.Begin);
//                using (var bw = new BinaryWriter(fs))
//                {
//                    ver.Serialize(bw);
//                }
//            }

//            File.WriteAllText(versionListPath + ".dump.txt", ver.Dump());
//        }

//        internal static string GetHashUseEVerify(Stream stream)
//        {
//            if (s_VerifyBy == EVerifyBy.Crc32)
//            {
//                return Utility.HashUtility.GetCrc32Hash(stream);
//            }
//            else if (s_VerifyBy == EVerifyBy.Md5)
//            {
//                return Utility.HashUtility.GetMd5Hash(stream);
//            }

//            throw new NotImplementedException("hash function invalid.");
//        }

//        internal static bool VerifyHashUseEVerify(string hash, Stream stream)
//        {
//            if (s_VerifyBy == EVerifyBy.Crc32)
//            {
//                return Utility.HashUtility.VerifyCrc32Hash(hash, Utility.HashUtility.GetCrc32Hash(stream));
//            }
//            else if (s_VerifyBy == EVerifyBy.Md5)
//            {
//                return Utility.HashUtility.VerifyMd5Hash(hash, Utility.HashUtility.GetMd5Hash(stream));
//            }

//            throw new NotImplementedException("hash function invalid.");
//        }

//        public void Update(VersionManifest remote)
//        {
//            remoteManifestUrl = remote.remoteManifestUrl;
//            remoteVersionUrl = remote.remoteVersionUrl;
//            resVersion = remote.resVersion;
//            assets = remote.assets;
//        }

//        public static List<AssetInfo> DiffAssets(VersionManifest local, VersionManifest remote)
//        {
//            if (remote == null) return null;

//            if (local == null) return remote.assets.Values.ToList();

//            var diff = new List<AssetInfo>();
//            foreach (var otherAsset in remote.assets)
//            {
//                if (!local.assets.TryGetValue(otherAsset.Key, out var asset))
//                {
//                    diff.Add(otherAsset.Value);
//                }
//                else
//                {
//                    if (asset.IsNew(otherAsset.Value))
//                        diff.Add(otherAsset.Value);
//                }
//            }
//            return diff;
//        }

//        public bool IsValid()
//        {
//            return resVersion > 0 &&
//                assets != null &&
//                assets.Count > 0;
//        }

//        public string Dump()
//        {
//#if UNITY_EDITOR
//            var sb = new System.Text.StringBuilder(2048);

//            sb.Append("remoteVersionUrl: ").Append(remoteVersionUrl.ToString()).AppendLine();
//            sb.Append("remoteManifestUrl: ").Append(remoteManifestUrl.ToString()).AppendLine();
//            sb.Append("resVersion: ").Append(resVersion.ToString()).AppendLine();
//            sb.Append("appVersion: ").Append(appVersion.ToString()).AppendLine();
//            sb.Append("assetCount: ").Append(assets.Count).AppendLine();
//            sb.AppendLine();

//            foreach (var item in assets)
//            {
//                item.Value.Dump(sb);
//            }

//            return sb.ToString();
//#else
//            return null;
//#endif
//        }

//        public void Serialize(BinaryWriter writer)
//        {
//            if (!IsValid())
//                throw new Exception("version list is invalid.");

//            writer.Write(appVersion.ToString());
//            writer.Write(resVersion);
//            writer.Write(remoteVersionUrl);
//            writer.Write(remoteManifestUrl);

//            writer.Write(assets.Count);
//            foreach (var item in assets)
//            {
//                item.Value.Serialize(writer);
//            }
//        }

//        public void Deserialize(BinaryReader reader)
//        {
//            appVersion = new Version(reader.ReadString());
//            resVersion = reader.ReadInt32();
//            remoteVersionUrl = reader.ReadString();
//            remoteManifestUrl = reader.ReadString();

//            var count = reader.ReadInt32();
//            assets = new Dictionary<string, AssetInfo>(count, StringComparer.OrdinalIgnoreCase);
//            for (int i = 0; i < count; i++)
//            {
//                var assetInfo = new AssetInfo();
//                assetInfo.Deserialize(reader);
//                assets.Add(assetInfo.key, assetInfo);
//            }
//        }
//    }

//    public struct AssetInfo
//    {
//        /// <summary>
//        /// 资源的相对路径
//        /// </summary>
//        public string key;
//        /// <summary>
//        /// 资源文件的版本信息
//        /// </summary>
//        public string md5;
//        /// <summary>
//        /// 文件的字节尺寸，可用于大文件切片
//        /// </summary>
//        public long size;
//        /// <summary>
//        /// [可选] 文件的偏移，用于大文件切片
//        /// </summary>
//        public long offset;
//        /// <summary>
//        /// [可选] 文件是否压缩
//        /// </summary>
//        public bool compressed;

//        public bool IsValid()
//        {
//            return size > 0L &&
//                !string.IsNullOrEmpty(key) &&
//                !string.IsNullOrEmpty(md5);
//        }

//        public bool IsNew(AssetInfo other)
//        {
//            return string.Compare(md5, other.md5, StringComparison.OrdinalIgnoreCase) != 0;
//        }

//        public void Serialize(BinaryWriter writer)
//        {
//            writer.Write(key);
//            writer.Write(md5);
//            writer.Write(size);
//            writer.Write(offset);
//            writer.Write(compressed);
//        }

//        public void Deserialize(BinaryReader reader)
//        {
//            key = reader.ReadString();
//            md5 = reader.ReadString();
//            size = reader.ReadInt64();
//            offset = reader.ReadInt64();
//            compressed = reader.ReadBoolean();
//        }

//#if UNITY_EDITOR
//        public void Dump(System.Text.StringBuilder builder)
//        {
//            builder
//                .AppendLine(key)
//                .Append("   md5: ").AppendLine(md5)
//                .Append("   length: ").AppendLine(size.ToString())
//                .Append("   offset: ").AppendLine(offset.ToString())
//                .Append("   compressed: ").AppendLine(compressed.ToString());
//        }
//#endif

//        public override string ToString()
//        {
//            return $"{key}|{md5}";
//        }
//    }
//}