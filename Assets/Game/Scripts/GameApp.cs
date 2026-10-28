using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LitJson;


public class GameApp : UnitySingleton<GameApp>
{
    private void OnDestroy() => MessageDispatcher.Instance.Dispose();

    public void Init() {
        
        MessageFactory.Instance.InitMeesagePool();
        MessageDispatcher.Instance.Init();

        UIEventProcesser.Instance.Init();

        GM_DataMgr.Instance.Init();
    }

    public IEnumerator EnterGame() { // 进入游戏
        Debug.Log("EnterGame ...");

        UIMgr.Instance.ShowUIView("GUI/Prefabs/UIBattle");

        Debug.Log("End Game");
        yield break;
    }


    public void EnterLoginScene() {
        UIMgr.Instance.ShowUIView("GUI/Prefabs/UILogin");
    }

    public void EnterLoadingScene() {
        UIMgr.Instance.RemoveUIView("GUI/Prefabs/UILogin");
        UIMgr.Instance.ShowUIView("GUI/Prefabs/UILoading");

        // 加载游戏数据的命令
        PlayerProxy.Instance.DoReqPullingPlayerDataAction();
        // end
    }


    public void EnterSelectScene() {
        UIMgr.Instance.RemoveUIView("GUI/Prefabs/UILoading");
        UIMgr.Instance.ShowUIView("GUI/Prefabs/UISelectRole");
    }

    IEnumerator TestGame() {

        // 测试同步资源加载
        /*TextAsset t = ResMgr.Instance.LoadAssetSync<TextAsset>("Datas/fragment");
        Debug.Log(t.text);
        */
        // end


        // 异步加载资源
        /*var h = ResMgr.Instance.LoadAssetAsync<TextAsset>("Datas/fruit.csv");
        yield return h;
        t = h.AssetObject as TextAsset;
        h.Dispose();
        Debug.Log(t.text);*/
        // end

        // 测试场景加载
        yield return SceneMgr.Instance.IE_RunScene("Scenes/main.unity");
        // SceneMgr.Instance.RunScene("Scenes/main");
        // end
        /*
        // 测试Log日志
        Debugger.Log("test 1");
        Debugger.Log("test 2");
        Debugger.Log("test 3");
        Debugger.Log("test 4");
        Debugger.Log("test 5");
        */

        /*
        // 事件触发测试
        EventMgr.Instance.AddListener("test", this.OnTestCall);
        EventMgr.Instance.RemoveListener("test", this.OnTestCall);
        EventMgr.Instance.Emit("test", 77777);
        */

        /*
        // Timer测试
        TimerMgr.Instance.ScheduleOnce((object udata)=> {
            Debug.Log("one time ####");
        }, 5f);

        int timerID = TimerMgr.Instance.Schedule((object udata) => {
            Debug.Log("repeat time ####");
        }, -1, 1f, 5f);


        TimerMgr.Instance.ScheduleOnce((object udata) => {
            Debug.Log("cancel time ####");
            TimerMgr.Instance.UnShedule(timerID);
        }, 10f);
        // end
        */

        /*
        // 测试声音
        SoundMgr.Instance.PlayOneShot("Sounds/bestReward");
        // end
        */
        /*
        // Json序列化与反序列化的测试
        UserLogin userLogin = new UserLogin();
        userLogin.uname = "blake";
        userLogin.upwd = "123456";
        userLogin.status = -101;
        // end

        // 序列化
        string jsonStr = JsonMapper.ToJson(userLogin);
        Debug.Log(jsonStr);
        // end

        // 反序列化
        string jsonFileStr = ResMgr.Instance.LoadAssetSync<TextAsset>("Datas/JsonText").text;
        JsonData jsonData = JsonMapper.ToObject(jsonFileStr);
        // end
        JsonData resCode = jsonData["rst"]["rstMsg"].ToString();
        string resCodestring = ((string)resCode);
        Debug.Log(resCodestring);
        */
        /*
        ExcelDataMgr.Instance.ReadConfigData<fragment>(null, false);
        fragment data = ExcelDataMgr.Instance.GetConfigData<fragment>("3");
        Debug.Log(data.ID); // 100401
        */

        /*
        // http 测试,测试百度
        HttpUtils.Get("https://www.baidu.com", null, (string err, object body)=> { 
            if(err == null) {
                Debug.Log(body);
            }
        });
        */

        /*
        // 节点池测试
        NodePoolMgr.Instance.AddNodePool("Models/Cube.prefab", 10);
        GameObject cube = NodePoolMgr.Instance.Get("Models/Cube.prefab");

        Transform parent = GameObject.Find("Directional Light").transform;
        cube.transform.SetParent(parent, false);
        cube.transform.position = new Vector3(10, 10, 10);

        TimerMgr.Instance.ScheduleOnce((object udata) => {
            NodePoolMgr.Instance.Recycle("Models/Cube.prefab", cube);
        }, 5);
        // end
        */


        // 测试UI
        // UIMgr.Instance.ShowUIView("GUI/Prefabs/UIHome");

        yield break;
    }

    /*private void OnTestCall(string uname, object udata) {
        Debug.Log("OnTestCall");
        Debug.Log(udata);
    }*/
}
