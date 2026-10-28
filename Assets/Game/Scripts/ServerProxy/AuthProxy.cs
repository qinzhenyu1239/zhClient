using Game;
using Framework.Core.Utils;
using Game.Datas.Messages;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ResponesProcesser]
public class AuthProxy 
{
    public static AuthProxy Instance = null;

    
    public AuthProxy() {
        if (AuthProxy.Instance == null) {
            AuthProxy.Instance = this;
        }
        else {
            Debug.LogWarning("Auth Proxy has multiple Instance !!!");
        }
    }

    public void DoReqGuestLoginAction()
    {
        // 处理游客登录
        var req = new ReqGuestLogin();
        // req.guestKey = "BYCW1234567890123456789";
        req.guestKey = GM_DataMgr.Instance.guestKey;
        if (req.guestKey == null || req.guestKey.Length <= 0)
        {
            req.guestKey = DataUtils.RandString(32);
            GM_DataMgr.Instance.SaveGuestKey(req.guestKey);
        }


        req.channal = (int)Channal.SelfChannal;
        TcpSocketMgr.Instance.SendMsg(req);
        // end
    }

    [ResponesMapping]
    public void DoReqGuestLoginReturn(ResGuestLogin respones) {
        Debug.Log("DoReqGuestLoginReturn:" + respones.status);

        if (respones.status == (int)Respones.OK) {
            GM_DataMgr.Instance.account = respones.uinfo;
            GameApp.Instance.EnterLoadingScene();
        }
    }
}
