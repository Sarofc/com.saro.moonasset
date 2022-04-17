﻿using Saro.XAsset;
using Saro.XAsset.Build;
using System;
using System.Collections.Generic;

namespace AssetBundleBrowser.AssetBundleDataSource
{
    internal class XAssetABDataSource : ABDataSource
    {
        public BuildGroups BuildGroups
        {
            get
            {
                if (m_BuildGroups == null)
                    m_BuildGroups = BuildScript.GetBuildGroups();

                return m_BuildGroups;
            }
        }

        private BuildGroups m_BuildGroups;

        public static List<ABDataSource> CreateDataSources()
        {
            var op = new XAssetABDataSource();
            var retList = new List<ABDataSource>();
            retList.Add(op);
            return retList;
        }

        public string Name
        {
            get
            {
                return "XAsset";
            }
        }

        public string ProviderName
        {
            get
            {
                return "BuildGroups";
            }
        }

        public string[] GetAssetPathsFromAssetBundle(string assetBundleName)
        {
            return BuildGroups.GetAssetPathsFromAssetBundle(assetBundleName);
        }

        public string GetAssetBundleName(string assetPath)
        {
            return BuildGroups.GetAssetBundleName(assetPath);
        }

        public string GetImplicitAssetBundleName(string assetPath)
        {
            return BuildGroups.GetImplicitAssetBundleName(assetPath);
        }

        public string[] GetAllAssetBundleNames()
        {
            return BuildGroups.GetAllAssetBundleNames();
        }

        public bool IsReadOnly()
        {
            return true;
        }

        public void SetAssetBundleNameAndVariant(string assetPath, string bundleName, string variantName)
        { }

        public void RemoveUnusedAssetBundleNames()
        { }

        public bool CanSpecifyBuildTarget
        {
            get { return true; }
        }
        public bool CanSpecifyBuildOutputDirectory
        {
            get { return true; }
        }

        public bool CanSpecifyBuildOptions
        {
            get { return true; }
        }

        public bool BuildAssetBundles(ABBuildInfo info)
        {
            throw new NotImplementedException();
        }

        public void Reload()
        {
            BuildGroups.Asset2BundleCahce = null;
        }

        public string GetRealAssetBundleFolderPath()
        {
            return XAssetConfig.k_Editor_DlcOutputPath + "/" + XAssetConfig.k_AssetBundleFoler;
        }
    }

    //internal class AssetDatabaseABDataSource : ABDataSource
    //{
    //    public static List<ABDataSource> CreateDataSources()
    //    {
    //        var op = new AssetDatabaseABDataSource();
    //        var retList = new List<ABDataSource>();
    //        retList.Add(op);
    //        return retList;
    //    }

    //    public string Name {
    //        get {
    //            return "Default";
    //        }
    //    }

    //    public string ProviderName {
    //        get {
    //            return "Built-in";
    //        }
    //    }

    //    public string[] GetAssetPathsFromAssetBundle (string assetBundleName) {
    //        return AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
    //    }

    //    public string GetAssetBundleName(string assetPath) {
    //        var importer = AssetImporter.GetAtPath(assetPath);
    //        if (importer == null) {
    //            return string.Empty;
    //        }
    //        var bundleName = importer.assetBundleName;
    //        if (importer.assetBundleVariant.Length > 0) {
    //            bundleName = bundleName + "." + importer.assetBundleVariant;
    //        }
    //        return bundleName;
    //    }

    //    public string GetImplicitAssetBundleName(string assetPath) {
    //        return AssetDatabase.GetImplicitAssetBundleName (assetPath);
    //    }

    //    public string[] GetAllAssetBundleNames() {
    //        return AssetDatabase.GetAllAssetBundleNames ();
    //    }

    //    public bool IsReadOnly() {
    //        return false;
    //    }

    //    public void SetAssetBundleNameAndVariant (string assetPath, string bundleName, string variantName) {
    //        AssetImporter.GetAtPath(assetPath).SetAssetBundleNameAndVariant(bundleName, variantName);
    //    }

    //    public void RemoveUnusedAssetBundleNames() {
    //        AssetDatabase.RemoveUnusedAssetBundleNames ();
    //    }

    //    public bool CanSpecifyBuildTarget { 
    //        get { return true; } 
    //    }
    //    public bool CanSpecifyBuildOutputDirectory { 
    //        get { return true; } 
    //    }

    //    public bool CanSpecifyBuildOptions { 
    //        get { return true; } 
    //    }

    //    public bool BuildAssetBundles (ABBuildInfo info) {
    //        if(info == null)
    //        {
    //            Debug.Log("Error in build");
    //            return false;
    //        }

    //        var buildManifest = BuildPipeline.BuildAssetBundles(info.outputDirectory, info.options, info.buildTarget);
    //        if (buildManifest == null)
    //        {
    //            Debug.Log("Error in build");
    //            return false;
    //        }

    //        foreach(var assetBundleName in buildManifest.GetAllAssetBundles())
    //        {
    //            if (info.onBuild != null)
    //            {
    //                info.onBuild(assetBundleName);
    //            }
    //        }
    //        return true;
    //    }

    //    public void Reload()
    //    {

    //    }

    //    public string GetRealAssetBundleFolderPath()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

}
