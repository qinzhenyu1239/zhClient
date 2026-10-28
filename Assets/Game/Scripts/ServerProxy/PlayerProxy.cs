using Game;
using Framework.Core.Utils;
using Game.Datas.Messages;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ResponesProcesser]
public class PlayerProxy 
{
    public static PlayerProxy Instance = null;

    
    public PlayerProxy() {
        if (PlayerProxy.Instance == null) {
            PlayerProxy.Instance = this;
        }
        else {
            Debug.LogWarning("Player Proxy has multiple Instance !!!");
        }
    }

    public void DoReqPullingPlayerDataAction()
    {
        ReqPullingPlayerData req = new ReqPullingPlayerData();
        req.job = 0; // 任意职业;

        TcpSocketMgr.Instance.SendMsg(req);
    }

    [ResponesMapping]
    public void DoResPullingPlayerDataReturn(ResPullingPlayerData respones) {
        Debug.Log("DoResPullingPlayerDataAction:" + respones.status);

        if (respones.status == (int)Respones.OK) { // 直接成功的进入游戏，加载其它数据;
            Debug.Log("Load Player Data Success!!!!");
            return;
        }

        if (respones.status == (int)Respones.PlayerIsNotExist) { // 进入游戏选角的流程
            GameApp.Instance.EnterSelectScene();
            return;
        }
    }

    public void DoReqSelectPlayerAction(ReqSelectPlayer req)
    {
        // 提交请求到服务器
        TcpSocketMgr.Instance.SendMsg(req);
    }

    [ResponesMapping]
    public void DoResSelectPlayerReturn(ResSelectPlayer respones)
    {
        Debug.Log("DoResSelectPlayerAction:" + respones.status);

        if (respones.status == (int)Respones.OK) 
        {
            Debug.Log("Select Player Success!!!");
        }
    }
}
