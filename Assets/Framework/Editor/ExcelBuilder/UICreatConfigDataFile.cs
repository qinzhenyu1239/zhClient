using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class UICreatConfigDataFile : EditorWindow {

    static string writePath = "/Game/Scripts/Excels/";
    static Object selectObj;


    [MenuItem("Tools/生成Excel格式类")]
    static void ByWindow()
    {
        EditorWindow.GetWindow<UICreatConfigDataFile>();
    }

    private void OnGUI()
    {
        GUILayout.Label("设置配置数据文件的生成路径");
        writePath = GUILayout.TextField(writePath);
        GUILayout.Label("请选择一个合法的CSV文件");

        if (GUILayout.Button("生成C#协议文件"))
        {
            Debug.Log("生成C#协议文件----------");
            if (selectObj != null)
            {
                CreatConfigUitl.CreatConfigFile(selectObj, writePath);
            }

        }

        if (Selection.activeObject != null)
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path.ToLower().Substring(path.Length - 4, 4) == ".csv")
            {
                selectObj = Selection.activeObject;
                GUILayout.Label(path);
            }
            else {
                selectObj = null;
            }
        }
    }

    private void OnSelectionChange()
    {
        Repaint();
    }



}
