using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.EventSystems;

// 【参考设计】
public class UICtrl : MonoBehaviour {

    public  GameObject ViewNode(string viewName) {

        Transform tf = this.transform.Find(viewName);
        return tf.gameObject;
    }

    public T View<T>(string viewName) where T : Component { 

        Transform tf = this.transform.Find(viewName);
        return tf.GetComponent<T>();
    }

    public void AddButtonListener(string viewName, UnityAction onclick) {
        // GameObject obj = this.View(viewName);
        // Debug.Log(obj.name);
        Button bt = this.View<Button>(viewName);
        if (bt == null) {
            Debug.LogWarning("UI_manager add_button_listener: not Button Component!");
            return;
        }

        bt.onClick.AddListener(onclick);
    }

    public void AddEventTriggerPointDown(string viewName, 
                                         UnityAction<BaseEventData> callback)
    {
        EventTrigger trigger = this.View<EventTrigger>(viewName);

        
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback = new EventTrigger.TriggerEvent();

        trigger.triggers.Add(entry);
        entry.callback.AddListener((callback));
    }

    public void AddEventTriggerDrag(string viewName,
                                     UnityAction<BaseEventData> callback)
    {
        EventTrigger trigger = this.View<EventTrigger>(viewName);


        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.Drag;
        entry.callback = new EventTrigger.TriggerEvent();

        trigger.triggers.Add(entry);
        entry.callback.AddListener((callback));
    }

    public void AddEventTriggerEndDrag(string viewName,
                                 UnityAction<BaseEventData> callback)
    {
        EventTrigger trigger = this.View<EventTrigger>(viewName);


        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.EndDrag;
        entry.callback = new EventTrigger.TriggerEvent();

        trigger.triggers.Add(entry);
        entry.callback.AddListener((callback));
    }

    public void AddEventTriggerBeginDrag(string viewName,
                             UnityAction<BaseEventData> callback)
    {
        EventTrigger trigger = this.View<EventTrigger>(viewName);


        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.BeginDrag;
        entry.callback = new EventTrigger.TriggerEvent();

        trigger.triggers.Add(entry);
        entry.callback.AddListener((callback));
    }

    public void AddEventTriggerPointClick(string viewName,
                                     UnityAction<BaseEventData> callback)
    {
        EventTrigger trigger = this.View<EventTrigger>(viewName);


        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback = new EventTrigger.TriggerEvent();

        trigger.triggers.Add(entry);
        entry.callback.AddListener((callback));
    }

    // ......
}

public class UIMgr : UnitySingleton<UIMgr> {
    public GameObject canvas;
    
    public void Init() {
        
        this.canvas = GameObject.Find("Canvas");
        if (this.canvas == null) {
            Debug.LogWarning("UI manager load  Canvas failed!!!!!");
        }
        else {
            DontDestroyOnLoad(this.canvas);
        }

        GameObject ev = GameObject.Find("EventSystem");
        if (ev) {
            DontDestroyOnLoad(ev);
        }
    }

    public UICtrl ShowUIWindow(string uiWindowPath) {
        
        GameObject uiPrefab = (GameObject)ResMgr.Instance.LoadAssetSync<GameObject>(uiWindowPath);
        GameObject uiView = GameObject.Instantiate(uiPrefab);
        uiView.name = uiPrefab.name;

        Type type = Type.GetType(uiPrefab.name + "_UICtrl");
        UICtrl ctrl = (UICtrl)uiView.AddComponent(type);

        return ctrl;
    }

    public void RemoveUIWindow(string uiWindowPath) {
        int lastIndex = uiWindowPath.LastIndexOf("/");
        if (lastIndex > 0) {
            uiWindowPath = uiWindowPath.Substring(lastIndex + 1);
        }

        GameObject uiWindow = GameObject.Find(uiWindowPath);
        GameObject.DestroyImmediate(uiWindow);
    }

    public async Task<UICtrl> AwaitShowUIView(string viewPath, GameObject parent = null)
    {

        GameObject uiPrefab = await ResMgr.Instance.AwaitGetAsset<GameObject>(viewPath);
        GameObject uiView = GameObject.Instantiate(uiPrefab);
        uiView.name = uiPrefab.name;
        if (parent == null)
        {
            parent = this.canvas;
        }
        uiView.transform.SetParent(parent.transform, false);

        Type type = Type.GetType(uiPrefab.name + "_UICtrl");
        UICtrl ctrl = (UICtrl)uiView.AddComponent(type);

        return ctrl;
    }

    public UICtrl ShowUIView(string viewPath, GameObject parent = null) {
        
        GameObject uiPrefab = (GameObject)ResMgr.Instance.LoadAssetSync<GameObject>(viewPath);
        GameObject uiView = GameObject.Instantiate(uiPrefab);
        uiView.name = uiPrefab.name;
        if (parent == null) {
            parent = this.canvas;
        }
        uiView.transform.SetParent(parent.transform, false);
        
        Type type = Type.GetType(uiPrefab.name + "_UICtrl");
        UICtrl ctrl = (UICtrl)uiView.AddComponent(type);

        return ctrl;
    }

    public void RemoveUIView(string viewPath) {
        int lastIndex = viewPath.LastIndexOf("/");
        if (lastIndex > 0) {
            viewPath = viewPath.Substring(lastIndex + 1);
        }

        Transform view = this.canvas.transform.Find(viewPath);
        // Debug.Log(viewPath);
        if (view) {
            GameObject.DestroyImmediate(view.gameObject);
        }
    }

    public void RemoveAllViews() {
        UnityUtils.DestroyAllChildren(this.canvas.transform);
    }
}
