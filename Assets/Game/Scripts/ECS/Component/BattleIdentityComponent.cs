using Framework.ECS;

public enum BattleSide
{
    Ally,
    Enemy
}

[ComponentOf(typeof(BattleUnit))]
public sealed class BattleIdentityComponent : Entity
{
    public int UnitId { get; internal set; }
    public int CharacterId { get; internal set; }
    public string Name { get; internal set; }
    public BattleSide Side { get; internal set; }
}
