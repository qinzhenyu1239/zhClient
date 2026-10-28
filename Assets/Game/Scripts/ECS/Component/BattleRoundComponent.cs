using System.Collections.Generic;
using Framework.ECS;

public enum BattlePhase
{
    Preparation,
    Combat,
    Finished
}

[ComponentOf(typeof(BattleEntity))]
public sealed class BattleRoundComponent : Entity, IAwake, IUpdate, IDestroy
{
    public int RoundNumber { get; internal set; } = 1;
    public BattlePhase Phase { get; internal set; } = BattlePhase.Preparation;
    public double PreparationRemaining { get; internal set; }
    public BattleSide? Winner { get; internal set; }
    internal List<BattleUnit> ActionOrder { get; } = new List<BattleUnit>();
    internal int NextUnit { get; set; }
    internal double ActionRemaining { get; set; }
    public System.Random Random { get; internal set; }
    internal Queue<BattleActionResult> PendingActions { get; } = new Queue<BattleActionResult>();
}
