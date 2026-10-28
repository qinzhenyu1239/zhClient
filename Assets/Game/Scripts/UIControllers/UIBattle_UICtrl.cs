using System;
using UnityEngine;
using UnityEngine.UI;
using Framework.ECS;
using System.Collections.Generic;
using UnityEngine.Events;

// UIMgr attaches this controller when opening GUI/Prefabs/UIBattle.
public class UIBattle_UICtrl : UICtrl
{
    public BattleFormationView Formation { get; private set; }
    public BattleEntity Battle { get; private set; }
    private Button startBattleButton;
    private string lastAction = "等待单位行动";
    private BattleUnit selectedUnit;
    private readonly List<Button> skillButtons = new List<Button>();
    private readonly List<KeyValuePair<Button, UnityAction>> slotListeners = new List<KeyValuePair<Button, UnityAction>>();
    private Button cancelSkillButton;
    private string selectionMessage = "点击我方人物选择技能";

    private void Awake()
    {
        Formation = GetComponent<BattleFormationView>();
        startBattleButton = View<Button>("Content/StartBattleBtn");
        startBattleButton.onClick.AddListener(StartBattle);
        Formation.Initialize();
        for (int i = 0; i < Formation.Slots.Count; i++)
        {
            int slot = i;
            Button button = Formation.Slots[i].GetComponent<Button>();
            if (button == null) continue;
            UnityAction listener = () => SelectUnit(slot);
            button.onClick.AddListener(listener);
            slotListeners.Add(new KeyValuePair<Button, UnityAction>(button, listener));
        }
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            skillButtons.Add(CreateSkillButton("Skill" + (i + 1), -180 + i * 132, () => QueueSkill(index)));
        }
        cancelSkillButton = CreateSkillButton("CancelSkill", 216, CancelSkill);
        cancelSkillButton.GetComponentInChildren<Text>().text = "取消技能";
        View<Button>("Content/AutoFormationBtn").interactable = false;
        Text hint = View<Text>("Content/HintText");
        hint.fontSize = 12;
        hint.horizontalOverflow = HorizontalWrapMode.Wrap;
        hint.rectTransform.sizeDelta = new Vector2(550, 44);
        RefreshSelection();
    }

    private Button CreateSkillButton(string name, float x, UnityAction callback)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        root.layer = gameObject.layer;
        root.transform.SetParent(View<RectTransform>("Content/BattlePanel"), false);
        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(118, 36);
        rect.anchoredPosition = new Vector2(x, 210);
        root.GetComponent<Image>().color = new Color32(32, 89, 108, 255);
        var child = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        child.layer = gameObject.layer;
        child.transform.SetParent(root.transform, false);
        var labelRect = child.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        var label = child.GetComponent<Text>();
        label.font = View<Text>("Content/BattlePanel/Title").font;
        label.fontSize = 15;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        var button = root.GetComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        button.onClick.AddListener(callback);
        return button;
    }

    private int SelectedSkillId(int index) => selectedUnit?.Skills == null ? 0 :
        index == 0 ? selectedUnit.Skill1Id : index == 1 ? selectedUnit.Skill2Id : selectedUnit.Skill3Id;

    private void SelectUnit(int slot)
    {
        BattleUnit unit = Battle?.GetUnitAt(slot);
        if (unit == null || !unit.IsAlive || unit.Side != BattleSide.Ally) return;
        selectedUnit = unit;
        selectionMessage = "选择技能后立即扣点，替换该人物下一次普攻";
        RefreshSelection();
    }

    private void QueueSkill(int index)
    {
        if (selectedUnit?.Skills == null) return;
        int id = SelectedSkillId(index);
        selectionMessage = selectedUnit.Skills.TryQueue(id, out string reason)
            ? "已锁定技能 " + (index + 1) + "：" + selectedUnit.Skills.GetDefinition(id).FormatDescription() : reason;
        RefreshSelection();
    }

    private void CancelSkill()
    {
        int refund = selectedUnit?.Skills.CancelPendingSkill() ?? 0;
        selectionMessage = "已取消技能，退还 " + refund + " 点";
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        if (selectedUnit != null && (!selectedUnit.IsAlive || selectedUnit.IsDisposed)) selectedUnit = null;
        Text title = View<Text>("Content/BattlePanel/Title");
        title.fontSize = 18;
        title.text = selectedUnit == null ? "战场" : selectedUnit.Name + " #" + selectedUnit.UnitId;
        for (int i = 0; i < skillButtons.Count; i++)
        {
            int id = SelectedSkillId(i);
            var skills = selectedUnit?.Skills;
            BattleSkillDefinition definition = skills.GetDefinition(id);
            skillButtons[i].interactable = skills != null && skills.CanQueue(id, out _);
            skillButtons[i].GetComponentInChildren<Text>().text =
                (skills?.PendingSkillId == id && id != 0 ? "✓ " : "") + "技能" + (i + 1) + " · " + (definition?.PointCost.ToString() ?? "—");
        }
        if (cancelSkillButton != null) cancelSkillButton.interactable = selectedUnit?.Skills?.PendingSkillId > 0;
        var pool = Battle?.GetComponent<BattleSkillPointComponent>();
        View<Text>("Content/AutoFormationBtn/Text").text = "技能点 " + (pool == null ? "—" : pool.Current + "/" + pool.PointsPerRound);
        View<Text>("Content/HintText").text = selectedUnit == null ? "点击我方人物选择技能 · 每回合重置30点" :
            selectedUnit.HasActedThisRound ? "该人物本回合已行动，下回合可选择技能" : selectionMessage;
    }

    public void StartBattle()
    {
        try
        {
            character config = ExcelDataMgr.Instance.GetConfigData<character>(BattleEntity.BloodWerewolfId.ToString());
            EcsRuntime.Instance.Init();
            var skills = ExcelDataMgr.Instance.GetConfigDatas<skill>(null);
            BattleEntity battle = BattleInitializationSystem.CreateBattle(config, EcsRuntime.Instance.World, skillConfigs: skills);
            Formation.ResetCombatEffects();
            Formation.ShowBattle(battle);
            Battle?.Dispose();
            Battle = battle;
            selectedUnit = null;
            Battle.Enabled = isActiveAndEnabled;
            lastAction = "等待单位行动";
            RefreshBattleStatus();
        }
        catch (Exception exception)
        {
            View<Text>("Content/StatusText").text = "初始化失败，请检查人物表和战场配置";
            Debug.LogError("[Battle] Unit initialization failed: " + exception, this);
        }
    }

    private void Update()
    {
        if (Battle == null || Battle.IsDisposed) return;
        Formation.ShowBattle(Battle);
        foreach (BattleActionResult result in Battle.Round.DrainActions())
            OnActionResolved(result);
        RefreshBattleStatus();
        RefreshSelection();
    }

    private void OnActionResolved(BattleActionResult result)
    {
        string side = result.Unit.Side == BattleSide.Ally ? "我方" : "敌方";
        string movement = result.FromSlot == result.ToSlot ? "原地" : "前进一格";
        string attack = result.Target == null ? "无范围内目标" :
            result.Hit ? "造成 " + result.Damage.ToString("0.##") + " 伤害" : "攻击未命中";
        if (result.Kind == BattleActionKind.Bleed) attack = "流血 " + result.Damage.ToString("0.##");
        else if (result.Kind == BattleActionKind.Skill) attack = result.Message + (result.Target == null ? "" : "，" + attack);
        else if (!string.IsNullOrEmpty(result.Message)) attack = result.Message + "，" + attack;
        if (result.Hits.Count > 1) attack += "（" + result.Hits.Count + "次）";
        if (result.Healing > 0) attack += "，吸血 +" + result.Healing.ToString("0.##");
        if (selectedUnit == result.Unit) selectionMessage = "选择技能后立即扣点，替换该人物下一次普攻";
        lastAction = side + " " + result.Unit.UnitId + "：" + movement + "，" + attack;
        Debug.Log("[Battle] 第 " + result.RoundNumber + " 回合 · " + lastAction);
        Formation.ShowBattle(Battle);
        Formation.PlayActionEffect(result);
    }

    private void RefreshBattleStatus()
    {
        BattleRoundComponent round = Battle.Round;
        if (round == null)
        {
            View<Text>("Content/StatusText").text = "未挂载回合组件";
            return;
        }
        View<Text>("Content/RoundText").text = "第 " + round.RoundNumber.ToString("00") + " 回合";
        if (round.Phase == BattlePhase.Preparation)
        {
            lastAction = "等待单位行动";
            View<Text>("Content/StatusText").text = "准备阶段：" + Math.Ceiling(round.PreparationRemaining) + " 秒";
        }
        else if (round.Phase == BattlePhase.Combat)
            View<Text>("Content/StatusText").text = "战斗阶段 · " + lastAction;
        else
            View<Text>("Content/StatusText").text = round.Winner == null ? "战斗结束：平局" : round.Winner == BattleSide.Ally ? "战斗结束：我方获胜" : "战斗结束：敌方获胜";
        View<Text>("Content/BattlePanel/CountText").text = "我方 " + Battle.AliveCount(BattleSide.Ally) +
            " / 敌方 " + Battle.AliveCount(BattleSide.Enemy);
    }

    private void OnDestroy()
    {
        if (startBattleButton != null) startBattleButton.onClick.RemoveListener(StartBattle);
        foreach (var pair in slotListeners) if (pair.Key != null) pair.Key.onClick.RemoveListener(pair.Value);
        Battle?.Dispose();
    }

    private void OnEnable() { if (Battle != null && !Battle.IsDisposed) Battle.Enabled = true; }
    private void OnDisable() { if (Battle != null && !Battle.IsDisposed) Battle.Enabled = false; }
}
