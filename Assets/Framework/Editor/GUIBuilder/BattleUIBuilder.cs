using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Editor-only panel authoring. BattleFormationView instantiates the slot prefab at runtime.
public static class BattleUIBuilder
{
    public const string PrefabPath = "Assets/Game/AssetsPacakge/GUI/Prefabs/UIBattle.prefab";
    public const string SlotPrefabPath = "Assets/Game/AssetsPacakge/GUI/Prefabs/Items/AllySlot.prefab";
    private static readonly Color Background = Hex("101722");
    private static readonly Color Panel = Hex("172331");
    private static readonly Color Muted = Hex("8597AC");
    private static readonly Color Ally = Hex("53C8DE");
    private static readonly Color Enemy = Hex("F07878");

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !File.Exists(PrefabPath))
                Create();
        };
    }

    [MenuItem("Tools/Battle UI/Create or Select Prefab")]
    public static void Create()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null)
        {
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
            return;
        }

        GameObject root = Node("UIBattle", null, 0, 0, 1280, 720);
        try
        {
            Stretch(root.GetComponent<RectTransform>());
            GameObject bg = Box("Background", root.transform, 0, 0, 1280, 720, Background);
            Stretch(bg.GetComponent<RectTransform>());
            GameObject content = Node("Content", root.transform, 0, 0, 1280, 720);
            Transform t = content.transform;
            Box("Header", t, 0, 310, 1280, 100, Hex("141F2C"));
            Label("Title", t, "战斗布阵", -462, 322, 280, 40, 28, Color.white, TextAnchor.MiddleLeft);
            Label("Subtitle", t, "FORMATION  /  9 VS 9", -462, 289, 280, 24, 12, Muted, TextAnchor.MiddleLeft);
            Box("RoundPanel", t, 0, 313, 184, 60, Hex("223144"));
            Label("RoundText", t, "第 01 回合", 0, 313, 170, 44, 22, Hex("E2C891"));
            Button("ExitBtn", t, "退出战斗", 536, 313, 136, 44, Hex("293647"));
            Team(t, "Ally", -330, "我方阵容", Ally);
            Team(t, "Enemy", 330, "敌方阵容", Enemy);
            Box("Divider", t, 0, 7, 1, 402, Hex("2C3B4C"));
            Label("Versus", t, "VS", 0, 26, 90, 60, 32, Hex("E2C891"));
            Label("VersusHint", t, "交战区", 0, -22, 84, 30, 12, Muted);
            Box("Footer", t, 0, -305, 1280, 110, Hex("141F2C"));
            Label("HintText", t, "选择格子部署单位 · 每方最多上阵 9 名", -330, -289, 550, 30, 17, Hex("C5D0DD"), TextAnchor.MiddleLeft);
            Label("StatusText", t, "等待布阵", -330, -320, 550, 26, 13, Muted, TextAnchor.MiddleLeft);
            Button("AutoFormationBtn", t, "自动布阵", 278, -305, 144, 48, Hex("293E50"));
            Button("StartBattleBtn", t, "开始战斗", 484, -305, 224, 48, Hex("356D77"));

            ConfigureFormation(root);
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Validate(saved);
            Selection.activeObject = saved;
            Debug.Log("[BattleUI] Created " + PrefabPath + " (9 slots per side generated at runtime).");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void Team(Transform parent, string side, float x, string title, Color accent)
    {
        GameObject panel = Box(side + "Panel", parent, x, 2, 548, 488, Panel);
        Transform t = panel.transform;
        Box("Accent", t, 0, 242, 548, 4, accent);
        Label("Title", t, title, -130, 210, 236, 38, 24, accent, TextAnchor.MiddleLeft);
        Label("CountText", t, "上阵 0 / 9", 162, 210, 160, 32, 16, Muted, TextAnchor.MiddleRight);
        GameObject grid = Node(side + "Grid", t, 0, -6, 500, 370);
        GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(156, 114);
        layout.spacing = new Vector2(16, 14);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 3;
        layout.childAlignment = TextAnchor.MiddleCenter;
        Label("FormationHint", t, "3 × 3 阵型  ·  点击空位上阵", 0, -218, 490, 24, 13, Muted);
    }

    private static void ConfigureFormation(GameObject root)
    {
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath);
        if (template == null) throw new InvalidOperationException("Missing slot prefab: " + SlotPrefabPath);
        var view = root.GetComponent<BattleFormationView>();
        if (view == null) view = root.AddComponent<BattleFormationView>();
        var data = new SerializedObject(view);
        data.FindProperty("slotPrefab").objectReferenceValue = template;
        data.FindProperty("allyGrid").objectReferenceValue = root.transform.Find("Content/AllyPanel/AllyGrid");
        data.FindProperty("enemyGrid").objectReferenceValue = root.transform.Find("Content/EnemyPanel/EnemyGrid");
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Tools/Battle UI/Configure Dynamic Slots")]
    public static void ConfigureDynamicSlots()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ConfigureFormation(root);
            Validate(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static GameObject Node(string name, Transform parent, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        return go;
    }

    private static GameObject Box(string name, Transform parent, float x, float y, float w, float h, Color color)
    {
        GameObject go = Node(name, parent, x, y, w, h);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = name == "Background";
        return go;
    }

    private static GameObject Label(string name, Transform parent, string value, float x, float y, float w, float h, int size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        GameObject go = Node(name, parent, x, y, w, h);
        Text text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return go;
    }

    private static GameObject Button(string name, Transform parent, string value, float x, float y, float w, float h, Color color)
    {
        GameObject go = Box(name, parent, x, y, w, h, color);
        go.GetComponent<Image>().raycastTarget = true;
        var button = go.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = go.GetComponent<Image>();
        var navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        if (!string.IsNullOrEmpty(value)) Label("Text", go.transform, value, 0, 0, w - 12, h, 18, Color.white);
        return go;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out Color color); return color; }

    [MenuItem("Tools/Battle UI/Validate Prefab")]
    public static void ValidatePrefab() { Validate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)); }

    private static void Validate(GameObject root)
    {
        if (root == null) throw new InvalidOperationException("UIBattle prefab is missing.");
        foreach (string side in new[] { "Ally", "Enemy" })
        {
            Transform grid = root.transform.Find("Content/" + side + "Panel/" + side + "Grid");
            if (grid == null || grid.childCount != 0) throw new InvalidOperationException(side + " grid must be empty for runtime generation.");
        }
        var view = root.GetComponent<BattleFormationView>();
        if (view == null) throw new InvalidOperationException("BattleFormationView is missing.");
        var data = new SerializedObject(view);
        foreach (string key in new[] { "slotPrefab", "allyGrid", "enemyGrid" })
            if (data.FindProperty(key).objectReferenceValue == null) throw new InvalidOperationException("Missing formation reference: " + key);
        Debug.Log("[BattleUI] Validation passed: empty grids and dynamic slot prefab bindings.");
    }
}
