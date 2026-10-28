using System.Collections;
using System.Collections.Generic;
using Game.Datas.Messages;
using UnityEngine;

public class UIEventProcesser : Singleton<UIEventProcesser>
{
    private Dictionary<int, EventMgr.OnEventAction> eventsMap = new Dictionary<int, EventMgr.OnEventAction>();
    public void Init() {
        
        this.eventsMap.Add((int)UIEvent.UIGuestLogin, this.OnUIHandleGuestLogin);
        this.eventsMap.Add((int)UIEvent.UISelectRole, this.OnUISelectPlayer);

        EventMgr.Instance.AddListener((int)EventType.UI, this.HandleUIEvent);
    }

    public void HandleUIEvent(int eventType, object udata, object param) {
        int uiEventType = (int)udata;
        if (this.eventsMap.ContainsKey(uiEventType)) {
            this.eventsMap[uiEventType](eventType, udata, param);
        }
    }

    private void OnUIHandleGuestLogin(int eventType, object udata, object param) {
        AuthProxy.Instance.DoReqGuestLoginAction();
    }

    private void OnUISelectPlayer(int eventType, object udata, object param) {
        ReqSelectPlayer req = (ReqSelectPlayer) param;
        PlayerProxy.Instance.DoReqSelectPlayerAction(req);
    }
}
