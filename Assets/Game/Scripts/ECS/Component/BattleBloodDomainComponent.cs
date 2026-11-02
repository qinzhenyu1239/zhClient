using Framework.ECS;

[ComponentOf(typeof(BattleUnit))]
public sealed class BattleBloodDomainComponent : Entity
{
    public int FirstRound { get; internal set; }
    public int LastRound { get; internal set; }
    public float AttackRatio { get; internal set; }
    public float DefenseRatio { get; internal set; }
    public float HealingRatio { get; internal set; }
    public int DefenseCap { get; internal set; }
}
