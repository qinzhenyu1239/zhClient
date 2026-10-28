using System;
using UnityEngine;
using UnityEngine.UI;

// One component per occupied slot, bound to that unit's independent combat state.
[DisallowMultipleComponent]
public sealed class BattleUnitView : MonoBehaviour
{
    public BattleUnit Unit { get; private set; }

#if UNITY_EDITOR
    [Serializable]
    private sealed class InspectorData
    {
        public System.Collections.Generic.List<string> components = new System.Collections.Generic.List<string>();
        public long entityId;
        public int unitId, characterId;
        public string name;
        public BattleSide side;
        public int slotNumber, column, row;
        public float currentHp;
        public int maxHp, attack, defense, battleType;
        public float speed, attackRange;
        public bool alive, hasActed;
        public int skill1Id, skill2Id, skill3Id, pendingSkillId, reservedSkillPoints, sharedSkillPoints;
        public int bleedTicks, nextBleedRound;
        public float bleedMaxHpRatio;
        public int lifestealStacks;
        public float lifestealRatio;
        public int comboFirstRound, comboLastRound, comboMinHits, comboMaxHits;
    }

    [SerializeField, Tooltip("运行时 ECS 数据的显示快照，每次刷新都会同步；修改这里不会改变实际战斗数据。")]
    private InspectorData runtimeData = new InspectorData();

    private void RefreshInspectorData()
    {
        BattleUnit unit = Unit;
        if (unit == null || unit.IsDisposed)
        {
            if (runtimeData.entityId != 0) runtimeData = new InspectorData();
            return;
        }
        runtimeData.components.Clear();
        foreach (var component in unit.Components) runtimeData.components.Add(component.GetType().Name);
        runtimeData.entityId = unit.Id;
        runtimeData.unitId = unit.UnitId;
        runtimeData.characterId = unit.CharacterId;
        runtimeData.name = unit.Name;
        runtimeData.side = unit.Side;
        runtimeData.slotNumber = unit.SlotIndex + 1;
        runtimeData.column = unit.Position == null ? 0 : unit.SlotIndex / BattleEntity.RowCount + 1;
        runtimeData.row = unit.Position == null ? 0 : unit.SlotIndex % BattleEntity.RowCount + 1;
        runtimeData.currentHp = unit.CurrentHp;
        runtimeData.maxHp = unit.MaxHp;
        runtimeData.alive = unit.IsAlive;
        runtimeData.hasActed = unit.HasActedThisRound;
        runtimeData.attack = unit.Attack;
        runtimeData.defense = unit.Defense;
        runtimeData.speed = unit.Speed;
        runtimeData.attackRange = unit.AttackRange;
        runtimeData.battleType = unit.BattleType;
        runtimeData.skill1Id = unit.Skill1Id;
        runtimeData.skill2Id = unit.Skill2Id;
        runtimeData.skill3Id = unit.Skill3Id;
        runtimeData.pendingSkillId = unit.Skills?.PendingSkillId ?? 0;
        runtimeData.reservedSkillPoints = unit.Skills?.ReservedCost ?? 0;
        runtimeData.sharedSkillPoints = unit.GetParent<BattleEntity>()?.GetComponent<BattleSkillPointComponent>()?.Current ?? 0;
        var bleed = unit.GetComponent<BattleBleedComponent>();
        runtimeData.bleedTicks = bleed?.RemainingTicks ?? 0;
        runtimeData.nextBleedRound = bleed?.NextTickRound ?? 0;
        runtimeData.bleedMaxHpRatio = bleed?.MaxHpRatio ?? 0;
        var lifesteal = unit.GetComponent<BattleLifestealComponent>();
        runtimeData.lifestealStacks = lifesteal?.Stacks ?? 0;
        runtimeData.lifestealRatio = (lifesteal?.Stacks ?? 0) * (lifesteal?.RatioPerStack ?? 0);
        var combo = unit.GetComponent<BattleComboAttackComponent>();
        runtimeData.comboFirstRound = combo?.FirstRound ?? 0;
        runtimeData.comboLastRound = combo?.LastRound ?? 0;
        runtimeData.comboMinHits = combo?.MinHits ?? 0;
        runtimeData.comboMaxHits = combo?.MaxHits ?? 0;
    }
#endif

    public void Bind(BattleUnit unit)
    {
        Unit = unit ?? throw new ArgumentNullException(nameof(unit));
        Refresh();
    }

    public void Clear()
    {
        Unit = null;
        Refresh();
    }

    public void Refresh()
    {
#if UNITY_EDITOR
        RefreshInspectorData();
#endif
        Render(transform, Unit);
    }

    internal static void ShowEmptySlot(GameObject slot)
    {
        Render(slot.transform, null);
    }

    private static void Render(Transform root, BattleUnit unit)
    {
        bool occupied = unit != null && !unit.IsDisposed && unit.Identity != null &&
            unit.Position != null && unit.Health != null && unit.IsAlive;
        SetVisible(root, "EmptyLabel", !occupied);
        SetVisible(root, "Portrait", occupied);
        SetVisible(root, "NameText", occupied);
        SetVisible(root, "HealthBar", occupied);
        SetVisible(root, "Selected", occupied);

        Color accent = occupied && unit.Side == BattleSide.Enemy
            ? new Color32(240, 120, 120, 255)
            : new Color32(83, 200, 222, 255);
        SetAccent(root, "Selected", accent);
        SetAccent(root, "HealthBar/Fill", accent);
        Transform indexNode = root.Find("IndexText");
        if (indexNode != null && indexNode.TryGetComponent(out Text index)) index.color = accent;
        Transform nameNode = root.Find("NameText");
        if (nameNode != null && nameNode.TryGetComponent(out Text name))
        {
            name.text = occupied ? unit.Name : string.Empty;
            name.color = accent;
        }
        Transform fillNode = root.Find("HealthBar/Fill");
        if (fillNode != null && fillNode.TryGetComponent(out Image fill))
            fill.fillAmount = occupied ? (float)unit.CurrentHp / unit.MaxHp : 0f;
    }

    private static void SetVisible(Transform slot, string path, bool visible)
    {
        Transform node = slot.Find(path);
        if (node != null) node.gameObject.SetActive(visible);
    }

    private static void SetAccent(Transform slot, string path, Color color)
    {
        Transform node = slot.Find(path);
        if (node != null && node.TryGetComponent(out Image image)) image.color = color;
    }
}
