using Framework.ECS;

[ComponentOf(typeof(BattleUnit))]
public sealed class BattleBleedComponent : Entity
{
    public BattleUnit Source { get; internal set; }
    public int NextTickRound { get; internal set; }
    public int RemainingTicks { get; internal set; }
    public float MaxHpRatio { get; internal set; }
}
