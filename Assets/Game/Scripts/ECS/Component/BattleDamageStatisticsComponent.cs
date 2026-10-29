using Framework.ECS;

[ComponentOf(typeof(BattleUnit))]
public sealed class BattleDamageStatisticsComponent : Entity
{
    public double TotalDamage { get; internal set; }
}
