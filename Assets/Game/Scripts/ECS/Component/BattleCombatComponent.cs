using Framework.ECS;

[ComponentOf(typeof(BattleUnit))]
public sealed class BattleCombatComponent : Entity
{
    public int Attack { get; internal set; }
    public int Defense { get; internal set; }
    public float Speed { get; internal set; }
    public float AttackRange { get; internal set; }
    public int BattleType { get; internal set; }
}
