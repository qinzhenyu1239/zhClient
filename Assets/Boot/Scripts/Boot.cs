using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

public class Boot : UnitySingleton<Boot>
{
    public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;
    public string remoteURL = "http://127.0.0.1:6080";
    public bool isShowDebugLogWindow = false;

    public string serverIp = "127.0.0.1";
    public int port = 6080;
    public bool connectOnStartup = false;

    public override void Awake() {
        base.Awake();
        
        Application.targetFrameRate = 60;
        Application.runInBackground = true;

        this.StartCoroutine(this.BootStartup());
    }

    private IEnumerator CheckHotUpdate() {
        YooAssetHotUpdate.Instance.Init(this.PlayMode, this.remoteURL);
        yield return YooAssetHotUpdate.Instance.GameHotUpdate();
    }

    private IEnumerator InitFramework() {
        Debug.Log("InitFramework ...");

        this.gameObject.AddComponent<ResMgr>().Init();
        this.gameObject.AddComponent<EventMgr>().Init();
        this.gameObject.AddComponent<TimerMgr>().Init();
        this.gameObject.AddComponent<SoundMgr>().Init();
        this.gameObject.AddComponent<NodePoolMgr>().Init();
        this.gameObject.AddComponent<UIMgr>().Init();
        this.gameObject.AddComponent<TcpSocketMgr>().Init(this.serverIp, this.port);

        ExcelDataMgr.Instance.Init();

        this.gameObject.AddComponent<SceneMgr>().Init();
        this.gameObject.AddComponent<GameApp>().Init();
        if (this.connectOnStartup) TcpSocketMgr.Instance.ConnectToServer();


#if RELEASE_BUILD
#else
        if (this.isShowDebugLogWindow) {
            this.gameObject.AddComponent<Debugger>().Init();
        }
    
#endif

        yield break;
    }

    IEnumerator BootStartup() {
        // 初始化YooAsset
        YooAssets.Initialize();
        YooAssets.SetOperationSystemMaxTimeSlice(30);
        // end

        // 检查热更新
        yield return this.CheckHotUpdate();
        // end

        // 框架初始化
        yield return this.InitFramework();
        // end

        // 进入游戏
        yield return GameApp.Instance.EnterGame();
        // end 
    }
}
