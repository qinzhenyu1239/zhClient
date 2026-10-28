using Framework.ECS;

[ComponentOf(typeof(BattleEntity))]
public sealed class BattleSkillPointComponent : Entity, IAwake
{
    public int PointsPerRound { get; internal set; }
    public int Current { get; internal set; }
    public int RoundNumber { get; internal set; }
}
