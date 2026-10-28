using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventMgr : UnitySingleton<EventMgr>
{
    public delegate void OnEventAction(int type, object udata, object udata2 = null);
    private Dictionary<int, OnEventAction> eventActions = null;
    public void Init() {
        this.eventActions = new Dictionary<int, OnEventAction>();
    }

    public void AddListener(int eventType, OnEventAction onEvent) {
        if (this.eventActions.ContainsKey(eventType)) {
            this.eventActions[eventType] += onEvent;
        }
        else {
            this.eventActions[eventType] = onEvent;
        }
    }

    public void RemoveListener(int eventType, OnEventAction onEvent) {
        if (this.eventActions.ContainsKey(eventType)) {
            this.eventActions[eventType] -= onEvent;
        }

        /*if (this.eventActions[eventName] == null) {
            this.eventActions.Remove(eventName);
        }*/
    }

    public void Emit(int eventType, object udata, object udata2 = null) {
        if (this.eventActions.ContainsKey(eventType)) {
            if (this.eventActions[eventType] != null) {
                this.eventActions[eventType](eventType, udata, udata2);
            }
        }
    }
}
