using Framework.ECS;

public sealed class BattleUnit : Entity, IDestroy
{
    public BattleIdentityComponent Identity => GetComponent<BattleIdentityComponent>();
    public BattlePositionComponent Position => GetComponent<BattlePositionComponent>();
    public BattleHealthComponent Health => GetComponent<BattleHealthComponent>();
    public BattleCombatComponent Combat => GetComponent<BattleCombatComponent>();
    public BattleSkillComponent Skills => GetComponent<BattleSkillComponent>();
    public BattleActionComponent Action => GetComponent<BattleActionComponent>();

    // Read-only queries used by presentation and Inspector; each value has one component owner.
    public int UnitId => Identity?.UnitId ?? 0;
    public int CharacterId => Identity?.CharacterId ?? 0;
    public string Name => Identity?.Name ?? string.Empty;
    public BattleSide Side => Identity?.Side ?? BattleSide.Ally;
    public int SlotIndex => Position?.SlotIndex ?? -1;
    public int MaxHp => Health?.MaxHp ?? 0;
    public float CurrentHp => Health?.CurrentHp ?? 0;
    public bool IsAlive => !IsDisposed && Health != null && Health.CurrentHp > 0;
    public bool HasActedThisRound => Action?.HasActedThisRound ?? false;
    public int Attack => Combat?.Attack ?? 0;
    public int Defense => Combat?.Defense ?? 0;
    public float Speed => Combat?.Speed ?? 0;
    public float AttackRange => Combat?.AttackRange ?? 0;
    public int BattleType => Combat?.BattleType ?? 0;
    public int Skill1Id => Skills?.Skill1Id ?? 0;
    public int Skill2Id => Skills?.Skill2Id ?? 0;
    public int Skill3Id => Skills?.Skill3Id ?? 0;
}
