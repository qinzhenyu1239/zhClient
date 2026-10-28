using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

using UnityEngine;
using YooAsset;

public class YooAssetHotUpdate : Singleton<YooAssetHotUpdate>
{
	private string PackageVersion;

	private string remoteURL = "http://127.0.0.1:6080/";
    private EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

	public ResourceDownloaderOperation Downloader { set; get; }

	private class RemoteServices : IRemoteServices
	{
		private readonly string _defaultHostServer;
		private readonly string _fallbackHostServer;

		public RemoteServices(string defaultHostServer, string fallbackHostServer)
		{
			_defaultHostServer = defaultHostServer;
			_fallbackHostServer = fallbackHostServer;
		}
		string IRemoteServices.GetRemoteMainURL(string fileName)
		{
			return $"{_defaultHostServer}/{fileName}";
		}
		string IRemoteServices.GetRemoteFallbackURL(string fileName)
		{
			return $"{_fallbackHostServer}/{fileName}";
		}
	}

	private class GameDecryptionServices : IDecryptionServices
	{
		public ulong LoadFromFileOffset(DecryptFileInfo fileInfo)
		{
			return 32;
		}

		public byte[] LoadFromMemory(DecryptFileInfo fileInfo)
		{
			throw new NotImplementedException();
		}

		public Stream LoadFromStream(DecryptFileInfo fileInfo)
		{
			BundleStream bundleStream = new BundleStream(fileInfo.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			return bundleStream;
		}

		public uint GetManagedReadBufferSize()
		{
			return 1024;
		}
	}

	private class GameQueryServices : IQueryServices
	{
		public const string RootFolderName = "yoo";
	
	public DeliveryFileInfo GetDeliveryFileInfo(string packageName, string fileName)
		{
			throw new System.NotImplementedException();
		}

		public bool QueryDeliveryFiles(string packageName, string fileName)
		{
			return false;
		}

		private static bool FileExists(string packageName, string fileName)
		{
			string filePath = Path.Combine(Application.streamingAssetsPath, RootFolderName, packageName, fileName);
			return File.Exists(filePath);
		}

		public bool QueryStreamingAssets(string packageName, string fileName)
		{
			// 注意：fileName包含文件格式
			return GameQueryServices.FileExists(packageName, fileName);
		}
	}

	private string GetHostServerURL()
	{
		//string hostServerIP = "http://10.0.2.2"; //安卓模拟器地址
		string hostServerIP = "http://127.0.0.1";
		string appVersion = "v1.0";

#if UNITY_EDITOR
		if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
			return $"{hostServerIP}/CDN/Android/{appVersion}";
		else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
			return $"{hostServerIP}/CDN/IPhone/{appVersion}";
		else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
			return $"{hostServerIP}/CDN/WebGL/{appVersion}";
		else
			return $"{hostServerIP}/CDN/PC/{appVersion}";
#else
		if (Application.platform == RuntimePlatform.Android)
			return $"{hostServerIP}/CDN/Android/{appVersion}";
		else if (Application.platform == RuntimePlatform.IPhonePlayer)
			return $"{hostServerIP}/CDN/IPhone/{appVersion}";
		else if (Application.platform == RuntimePlatform.WebGLPlayer)
			return $"{hostServerIP}/CDN/WebGL/{appVersion}";
		else
			return $"{hostServerIP}/CDN/PC/{appVersion}";
#endif
	}

	public void Init(EPlayMode playMode, string url) {
        this.remoteURL = url;
        this.PlayMode = playMode;
    }

	private IEnumerator InitPackage()
	{
		yield return new WaitForSeconds(1f);

		var playMode = this.PlayMode;

		// 创建默认的资源包
		string packageName = ResMgr.DefaultPackage;
		var package = YooAssets.TryGetPackage(packageName);
		if (package == null) {
			package = YooAssets.CreatePackage(packageName);
			YooAssets.SetDefaultPackage(package);
		}
		else {
			YooAssets.SetDefaultPackage(package);
		}

		// 编辑器下的模拟模式
		InitializationOperation initializationOperation = null;
		if (playMode == EPlayMode.EditorSimulateMode)
		{
			var createParameters = new EditorSimulateModeParameters();
			createParameters.SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild(packageName);
			initializationOperation = package.InitializeAsync(createParameters);
		}

		// 单机运行模式
		if (playMode == EPlayMode.OfflinePlayMode)
		{
			var createParameters = new OfflinePlayModeParameters();
			createParameters.DecryptionServices = new GameDecryptionServices();
			initializationOperation = package.InitializeAsync(createParameters);
		}

		// 联机运行模式
		if (playMode == EPlayMode.HostPlayMode)
		{
			string defaultHostServer = GetHostServerURL();
			string fallbackHostServer = GetHostServerURL();
			var createParameters = new HostPlayModeParameters();
			createParameters.DecryptionServices = new GameDecryptionServices();
			createParameters.QueryServices = new GameQueryServices();
			createParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
			initializationOperation = package.InitializeAsync(createParameters);
		}

		// WebGL运行模式
		if (playMode == EPlayMode.WebPlayMode)
		{
			string defaultHostServer = GetHostServerURL();
			string fallbackHostServer = GetHostServerURL();
			var createParameters = new WebPlayModeParameters();
			createParameters.DecryptionServices = new GameDecryptionServices();
			createParameters.QueryServices = new GameQueryServices();
			createParameters.RemoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
			initializationOperation = package.InitializeAsync(createParameters);
		}

		yield return initializationOperation;
		if (initializationOperation.Status == EOperationStatus.Succeed)
		{
			Debug.Log("InitPackage Success!");
		}
		else
		{
			Debug.LogWarning($"{initializationOperation.Error}");
		}
	}

	private IEnumerator GetStaticVersion()
	{
		yield return new WaitForSecondsRealtime(0.5f);

		var package = YooAssets.GetPackage("DefaultPackage");
		var operation = package.UpdatePackageVersionAsync();
		yield return operation;

		if (operation.Status == EOperationStatus.Succeed) {
			this.PackageVersion = operation.PackageVersion;
			Debug.Log($"远端最新版本为: {operation.PackageVersion}");
		}
		else
		{
			Debug.LogWarning(operation.Error);
		}
	}

	private IEnumerator UpdateManifest()
	{
		yield return new WaitForSecondsRealtime(0.5f);

		bool savePackageVersion = true;
		var package = YooAssets.GetPackage("DefaultPackage");
		var operation = package.UpdatePackageManifestAsync(this.PackageVersion, savePackageVersion);
		yield return operation;

		if (operation.Status == EOperationStatus.Succeed) {
			Debug.Log("UpdateManifest Success !");
		}
		else
		{
			Debug.LogWarning(operation.Error);
		}
	}

	IEnumerator DownloadFiles()
	{
		yield return new WaitForSecondsRealtime(0.5f);

		int downloadingMaxNum = 10;
		int failedTryAgain = 3;
		var downloader = YooAssets.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);
		this.Downloader = downloader;

		if (downloader.TotalDownloadCount == 0)
		{
			Debug.Log("Not found any download files !");
			yield break;
			// _machine.ChangeState<FsmDownloadOver>();
		}
		else
		{
			//A total of 10 files were found that need to be downloaded
			Debug.Log($"Found total {downloader.TotalDownloadCount} files that need download ！");

			
			// 注册下载回调
			downloader.OnDownloadErrorCallback = this.OnDownloadError;
			downloader.OnDownloadProgressCallback = this.OnDownloadProgress;
			downloader.BeginDownload();
			yield return downloader;

			// 检测下载结果
			if (downloader.Status != EOperationStatus.Succeed)
				yield break;
		}

		yield break;
	}

	void OnDownloadProgress(int totalDownloadCount, int currentDownloadCount, long totalDownloadBytes, long currentDownloadBytes) { 
	}

	void OnDownloadError(string fileName, string error) {
	}

	IEnumerator ClearCache() {
		var package = YooAsset.YooAssets.GetPackage("DefaultPackage");
		var operation = package.ClearUnusedCacheFilesAsync();
		yield return operation;
	}
	public IEnumerator GameHotUpdate() {
		yield return this.InitPackage();
		yield return this.GetStaticVersion();
		yield return this.UpdateManifest();
		yield return this.DownloadFiles();
		yield return this.ClearCache();
	}
    
}



