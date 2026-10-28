using Framework.ECS;

[ComponentOf(typeof(BattleUnit))]
public sealed class BattleHealthComponent : Entity
{
    public int MaxHp { get; internal set; }
    public float CurrentHp { get; internal set; }
}
