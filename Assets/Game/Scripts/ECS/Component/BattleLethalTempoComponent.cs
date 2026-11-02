using Framework.ECS;

[ComponentOf(typeof(BattleUnit))]
public sealed class BattleLethalTempoComponent : Entity
{
    public BattleUnit Target { get; internal set; }
    public int FirstRound { get; internal set; }
    public int InitialHits { get; internal set; }
    public int ExtraHitsPerRound { get; internal set; }
    public float DamageRatio { get; internal set; }
}
