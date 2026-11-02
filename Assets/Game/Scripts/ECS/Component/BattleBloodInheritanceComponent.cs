using Framework.ECS;

[ComponentOf(typeof(BattleUnit))]
public sealed class BattleBloodInheritanceComponent : Entity
{
    public int AttackBonus { get; internal set; }
    public int DefenseBonus { get; internal set; }
}
