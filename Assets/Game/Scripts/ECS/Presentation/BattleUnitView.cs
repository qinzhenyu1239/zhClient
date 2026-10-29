using System;
using UnityEngine;
using UnityEngine.UI;

// One component per occupied slot, bound to that unit's independent combat state.
[DisallowMultipleComponent]
public sealed class BattleUnitView : MonoBehaviour
{
    public BattleUnit Unit { get; private set; }
    private Sprite defaultPortrait;
    private Sprite thunderPortrait;
    private Sprite moonPortrait;
    private Sprite judicatorPortrait;
    private bool portraitsLoaded;

    private void RefreshPortrait()
    {
        Transform node = transform.Find("Portrait");
        if (node == null || !node.TryGetComponent(out Image image)) return;
        if (!portraitsLoaded)
        {
            defaultPortrait = image.sprite;
            thunderPortrait = ResMgr.Instance.LoadAssetSync<Sprite>("Images/ThunderGod.png");
            moonPortrait = ResMgr.Instance.LoadAssetSync<Sprite>("Images/MoonlightGoddess.png");
            judicatorPortrait = ResMgr.Instance.LoadAssetSync<Sprite>("Images/Judicator.png");
            portraitsLoaded = true;
        }
        image.sprite = Unit?.CharacterId == BattleEntity.JudicatorId ? judicatorPortrait :
            Unit?.CharacterId == BattleEntity.MoonlightGoddessId ? moonPortrait :
            Unit?.CharacterId == BattleEntity.ThunderGodId ? thunderPortrait : defaultPortrait;
        image.preserveAspect = true;
    }

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
        public float shield;
        public int shieldDefenseBonus, judgmentFirstRound, judgmentLastRound;
        public float judgmentThresholdPercent;
        public int silenceFirstRound, silenceLastRound, cloudRemainingStrikes;
        public int maxHp, attack, defense, battleType;
        public float speed, attackRange;
        public bool alive, hasActed;
        public int skill1Id, skill2Id, skill3Id, pendingSkillId, reservedSkillPoints, sharedSkillPoints;
        public int bleedTicks, nextBleedRound;
        public float bleedMaxHpRatio;
        public int lifestealStacks;
        public float lifestealRatio;
        public int comboFirstRound, comboRemainingAttacks, comboMinHits, comboMaxHits;
        public int moonMarkStacks, moonShowerRemainingMarks;
        public int skill1CooldownRounds, skill2CooldownRounds, skill3CooldownRounds;
        public bool assassinHasInfiltrated;
        public bool assassinOpeningInfiltrationEligible;
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
        runtimeData.shield = unit.GetComponent<BattleShieldComponent>()?.Amount ?? 0;
        runtimeData.shieldDefenseBonus = unit.GetComponent<BattleShieldComponent>()?.DefenseBonus ?? 0;
        var judgment = unit.GetComponent<BattleJudgmentComponent>();
        runtimeData.judgmentFirstRound = judgment?.FirstRound ?? 0;
        runtimeData.judgmentLastRound = judgment?.LastRound ?? 0;
        runtimeData.judgmentThresholdPercent = judgment?.ThresholdPercent ?? 0;
        var silence = unit.GetComponent<BattleSilenceComponent>();
        runtimeData.silenceFirstRound = silence?.FirstRound ?? 0;
        runtimeData.silenceLastRound = silence?.LastRound ?? 0;
        runtimeData.cloudRemainingStrikes = unit.GetComponent<BattleThundercloudComponent>()?.RemainingStrikes ?? 0;
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
        runtimeData.skill1CooldownRounds = unit.Skills.CooldownRemaining(unit.Skill1Id);
        runtimeData.skill2CooldownRounds = unit.Skills.CooldownRemaining(unit.Skill2Id);
        runtimeData.skill3CooldownRounds = unit.Skills.CooldownRemaining(unit.Skill3Id);
        runtimeData.pendingSkillId = unit.Skills?.PendingSkillId ?? 0;
        runtimeData.reservedSkillPoints = unit.Skills?.ReservedCost ?? 0;
        runtimeData.sharedSkillPoints = unit.GetParent<BattleEntity>()?.GetComponent<BattleSkillPointComponent>().CurrentFor(unit.Side) ?? 0;
        var bleed = unit.GetComponent<BattleBleedComponent>();
        runtimeData.bleedTicks = bleed?.RemainingTicks ?? 0;
        runtimeData.nextBleedRound = bleed?.NextTickRound ?? 0;
        runtimeData.bleedMaxHpRatio = bleed?.MaxHpRatio ?? 0;
        var lifesteal = unit.GetComponent<BattleLifestealComponent>();
        runtimeData.lifestealStacks = lifesteal?.Stacks ?? 0;
        runtimeData.lifestealRatio = (lifesteal?.Stacks ?? 0) * (lifesteal?.RatioPerStack ?? 0);
        var combo = unit.GetComponent<BattleComboAttackComponent>();
        runtimeData.comboFirstRound = combo?.FirstRound ?? 0;
        runtimeData.comboRemainingAttacks = combo?.RemainingAttacks ?? 0;
        runtimeData.comboMinHits = combo?.MinHits ?? 0;
        runtimeData.comboMaxHits = combo?.MaxHits ?? 0;
        runtimeData.moonMarkStacks = unit.GetComponent<BattleMoonMarkComponent>()?.Stacks ?? 0;
        runtimeData.moonShowerRemainingMarks = unit.GetComponent<BattleMoonShowerComponent>()?.RemainingMarks ?? 0;
        runtimeData.assassinHasInfiltrated = unit.GetComponent<BattleAssassinComponent>()?.HasInfiltrated ?? false;
        runtimeData.assassinOpeningInfiltrationEligible = unit.GetComponent<BattleAssassinComponent>()?.OpeningInfiltrationEligible ?? false;
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
        if (Unit != null) RefreshPortrait();
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
            if (occupied)
            {
                float shield = unit.GetComponent<BattleShieldComponent>()?.Amount ?? 0;
                if (shield > 0) name.text += " 盾" + shield.ToString("0.#");
                if (unit.IsSilenced()) name.text += " 禁攻";
                int marks = unit.GetComponent<BattleMoonMarkComponent>()?.Stacks ?? 0;
                if (marks > 0) name.text += " 月印" + marks;
            }
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
