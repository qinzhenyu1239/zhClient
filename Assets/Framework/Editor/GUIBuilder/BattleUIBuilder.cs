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
    private static readonly Color Accent = Hex("53C8DE");

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
            Label("Subtitle", t, "BATTLEFIELD  /  9 × 3", -462, 289, 280, 24, 12, Muted, TextAnchor.MiddleLeft);
            Box("RoundPanel", t, 0, 313, 184, 60, Hex("223144"));
            Label("RoundText", t, "第 01 回合", 0, 313, 170, 44, 22, Hex("E2C891"));
            Button("ExitBtn", t, "退出战斗", 536, 313, 136, 44, Hex("293647"));
            Battlefield(t);
            Box("Footer", t, 0, -305, 1280, 110, Hex("141F2C"));
            Label("HintText", t, "选择格子部署单位 · 共 27 个战场格子", -330, -289, 550, 30, 17, Hex("C5D0DD"), TextAnchor.MiddleLeft);
            Label("StatusText", t, "等待布阵", -330, -320, 550, 26, 13, Muted, TextAnchor.MiddleLeft);
            Button("AutoFormationBtn", t, "自动布阵", 278, -305, 144, 48, Hex("293E50"));
            Button("StartBattleBtn", t, "开始战斗", 484, -305, 224, 48, Hex("356D77"));

            ConfigureFormation(root);
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Validate(saved);
            Selection.activeObject = saved;
            Debug.Log("[BattleUI] Created " + PrefabPath + " (27 shared slots generated at runtime).");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void Battlefield(Transform parent)
    {
        GameObject panel = Box("BattlePanel", parent, 0, 2, 1200, 488, Panel);
        Transform t = panel.transform;
        Box("Accent", t, 0, 242, 1200, 4, Accent);
        Label("Title", t, "战场", -440, 210, 236, 38, 24, Accent, TextAnchor.MiddleLeft);
        Label("CountText", t, "共 27 格", 450, 210, 220, 32, 16, Muted, TextAnchor.MiddleRight);
        GameObject grid = Node("BattleGrid", t, 0, -6, 1160, 370);
        GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(120, 114);
        layout.spacing = new Vector2(10, 14);
        layout.startAxis = GridLayoutGroup.Axis.Vertical;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = BattleFormationView.ColumnCount;
        layout.childAlignment = TextAnchor.MiddleCenter;
        Label("FormationHint", t, "9 列 × 3 行 · 点击空位上阵", 0, -218, 1120, 24, 13, Muted);
    }

    private static void ConfigureFormation(GameObject root)
    {
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath);
        if (template == null) throw new InvalidOperationException("Missing slot prefab: " + SlotPrefabPath);
        var view = root.GetComponent<BattleFormationView>();
        if (view == null) view = root.AddComponent<BattleFormationView>();
        var data = new SerializedObject(view);
        data.FindProperty("slotPrefab").objectReferenceValue = template;
        data.FindProperty("battleGrid").objectReferenceValue = root.transform.Find("Content/BattlePanel/BattleGrid");
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
        Transform grid = root.transform.Find("Content/BattlePanel/BattleGrid");
        if (grid == null || grid.childCount != 0) throw new InvalidOperationException("Battle grid must be empty for runtime generation.");
        var layout = grid.GetComponent<GridLayoutGroup>();
        if (layout == null || layout.constraint != GridLayoutGroup.Constraint.FixedColumnCount ||
            layout.constraintCount != BattleFormationView.ColumnCount || layout.startAxis != GridLayoutGroup.Axis.Vertical)
            throw new InvalidOperationException("Battle grid must have 9 columns with 3 slots per column.");
        var view = root.GetComponent<BattleFormationView>();
        if (view == null) throw new InvalidOperationException("BattleFormationView is missing.");
        var data = new SerializedObject(view);
        foreach (string key in new[] { "slotPrefab", "battleGrid" })
            if (data.FindProperty(key).objectReferenceValue == null) throw new InvalidOperationException("Missing formation reference: " + key);
        Debug.Log("[BattleUI] Validation passed: shared 9-column grid and dynamic slot prefab binding.");
    }
}
