using Framework.ECS;

[ComponentOf(typeof(BattleUnit))]
public sealed class BattleComboAttackComponent : Entity
{
    public int FirstRound { get; internal set; }
    public int RemainingAttacks { get; internal set; }
    public int MinHits { get; internal set; }
    public int MaxHits { get; internal set; }
}
