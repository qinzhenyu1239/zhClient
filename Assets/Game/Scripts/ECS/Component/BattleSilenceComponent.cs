using Framework.ECS;

[ComponentOf(typeof(BattleUnit))]
public sealed class BattleSilenceComponent : Entity
{
    public int FirstRound { get; internal set; }
    public int LastRound { get; internal set; }
}
