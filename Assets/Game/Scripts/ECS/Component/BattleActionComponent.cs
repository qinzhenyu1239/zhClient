using Framework.ECS;

[ComponentOf(typeof(BattleUnit))]
public sealed class BattleActionComponent : Entity
{
    public bool HasActedThisRound { get; internal set; }
}
