using Framework.ECS;
using System.Collections.Generic;

[ComponentOf(typeof(BattleUnit))]
public sealed class BattleMoonShowerComponent : Entity
{
    public int RemainingMarks { get; internal set; }
    public double SecondsUntilMark { get; internal set; }
    public double Interval { get; internal set; }
    public int MaxMarksPerEnemy { get; internal set; }
    // Counts applications during this cast, even after marks detonate or targets move.
    internal Dictionary<BattleUnit, int> AppliedMarks { get; } = new Dictionary<BattleUnit, int>();
}
