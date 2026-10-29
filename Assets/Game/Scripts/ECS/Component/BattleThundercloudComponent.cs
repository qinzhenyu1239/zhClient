using Framework.ECS;

// Scheduled by the battle clock so a long frame cannot tick a newly cast cloud retroactively.
[ComponentOf(typeof(BattleUnit))]
public sealed class BattleThundercloudComponent : Entity
{
    public int RemainingStrikes { get; internal set; }
    public double SecondsUntilStrike { get; internal set; } = 1;
    public float AttackMultiplier { get; internal set; }
    public float KillShieldMultiplier { get; internal set; }
    public int ShieldRounds { get; internal set; }
    public float Range { get; internal set; }
}
