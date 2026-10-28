using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

public class ResMgr : UnitySingleton<ResMgr>
{
    public const string DefaultPackage = "DefaultPackage";
    public const string AssetsPackageRoot = "Assets/Game/AssetsPacakge/";
    public void Init() {
        
    }

    // Package + 资源的路径;
    public AssetOperationHandle LoadAssetAsync<T>(string assetPath, string packageName = null) where T : UnityEngine.Object
    {
        assetPath = Path.Combine(AssetsPackageRoot, assetPath);
        ResourcePackage package = null;
        if (packageName == null) {
            AssetOperationHandle handle = YooAssets.LoadAssetAsync<T>(assetPath);
            return handle;
        }

        package = YooAssets.TryGetPackage(packageName);
        if (package == null) {
            return null;
        }

        return package.LoadAssetAsync<T>(assetPath);
    }

    public SceneOperationHandle LoadSceneAsync(string scenePath, string packageName = null)
    {
        scenePath = Path.Combine(AssetsPackageRoot, scenePath);

        ResourcePackage package = null;

        if (packageName == null) {
            packageName = DefaultPackage;
        }

        package = YooAssets.TryGetPackage(packageName);
        if (package == null) {
            return null;
        }


        return package.LoadSceneAsync(scenePath);
    }

    public async Task AwaitLoadAllAssetInGroup(string assetPath, string packageName = null) {
        assetPath = Path.Combine(AssetsPackageRoot, assetPath);

        ResourcePackage package = null;

        if (packageName == null) {
            packageName = DefaultPackage;
        }

        package = YooAssets.TryGetPackage(packageName);
        if (package == null) {
            return;
        }

        AllAssetsOperationHandle h = package.LoadAllAssetsAsync(assetPath);
        await h.Task;

        h.Dispose();
        return;
    }

    public async Task<T> AwaitGetAsset<T>(string assetPath, string packageName = null) where T : UnityEngine.Object
    {
        assetPath = Path.Combine(AssetsPackageRoot, assetPath);

        ResourcePackage package = null;

        if (packageName == null)
        {
            packageName = DefaultPackage;
        }

        package = YooAssets.TryGetPackage(packageName);
        if (package == null)
        {
            return null;
        }

        AssetOperationHandle h = package.LoadAssetAsync(assetPath);
        await h.Task;

        T obj = h.AssetObject as T;

        h.Dispose();
        return obj;
    }

    public T LoadAssetSync<T>(string assetPath, string packageName = null) where T : UnityEngine.Object
    {
        assetPath = Path.Combine(AssetsPackageRoot, assetPath);

        ResourcePackage package = null;
        T assetObject = null;
        AssetOperationHandle handle = null;

        if (packageName == null)
        {
            handle = YooAssets.LoadAssetSync<T>(assetPath);
            if (handle != null) {
                assetObject = handle.AssetObject as T;
                handle.Dispose();
            }
            
            return assetObject;
        }

        package = YooAssets.TryGetPackage(packageName);
        if (package == null) {
            return null;
        }

        handle = package.LoadAssetSync<T>(assetPath);
        if (handle != null)
        {
            assetObject = handle.AssetObject as T;
            handle.Dispose();
        }

        return assetObject;
    }

    public void UnloadUnusedAssets(string packageName) {
        if (packageName == null) {
            packageName = DefaultPackage;
        }

        ResourcePackage package = null;
        package = YooAssets.TryGetPackage(packageName);
        if (package == null) {
            return;
        }

        package.UnloadUnusedAssets();
    } 
}
