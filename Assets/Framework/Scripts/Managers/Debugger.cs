using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if RELEASE_BUILD
class Debugger {
    public static void Log(object message) {
    }    
}
#else
class Debugger : MonoBehaviour{

    public static bool isCreated = false;
    private static Debugger Instance = null;

    private List<string> lines = new List<string>();
    private bool isDebugWindowShow = false;

    private float timeDelta = 0.5f;
    private float prevComputeTime = 0;
    private int iFrameCount = 0;
    private float fps = 0;

    public static void Log(object message) {
        string msgStr = message.ToString();
        Debug.Log(msgStr);

        if (Debugger.Instance != null) {
            Debugger.Instance.lines.Add(msgStr);
        }
    }

    public void Init() {
        Debugger.Instance = this;
        this.prevComputeTime = Time.realtimeSinceStartup;
        this.iFrameCount = 0;
    }


    public void OnGUI() {
        GUILayout.Space(40);
        // 一个显示/隐藏
        if (GUILayout.Button("ShowError")) {
            this.isDebugWindowShow = !this.isDebugWindowShow;
        }
        // end 

        // 一个是清理日志的按钮
        else if (GUILayout.Button("Clear")) {
            this.lines.Clear();
        }
        // end

        // 显示日志窗口
        Rect InfoWindowRect = new Rect(80, 20, Screen.width - 80, Screen.height - 40);
        if (this.isDebugWindowShow) {
            GUILayout.Window(1, InfoWindowRect, this.DebugErrorWindow, "Debug Log Window");
        }
        // end 

        // 显示我们当前的FPS
        GUI.Label(new Rect(0, Screen.height - 20, 200, 200), "FPS:" + this.fps.ToString("f2"));
        // end
    }

    private Vector2 errorInfoPos = new Vector2(0, 0);

    private void DebugErrorWindow(int id) {
        int width = Screen.width - 100;
        int height = Screen.height - 40;

        GUILayout.BeginScrollView(errorInfoPos, false, true, GUILayout.Width(width), GUILayout.Height(height));
        // GUILayout.Space(30);
        GUILayout.BeginVertical();

        for (int i = 0; i < this.lines.Count; i++) {
            string str = this.lines[this.lines.Count - 1 - i];
            GUILayout.Label(str, GUILayout.Width(width));
        }

        GUILayout.EndVertical();
        GUILayout.EndScrollView();
    }

    private void Update() {
        this.iFrameCount ++;
        if (Time.realtimeSinceStartup >= this.prevComputeTime + this.timeDelta) {
            this.fps = this.iFrameCount / (Time.realtimeSinceStartup - this.prevComputeTime);
            this.prevComputeTime = Time.realtimeSinceStartup;
            this.iFrameCount = 0;
        }
    }

}
#endif

