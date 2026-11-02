using System;
using Framework.ECS;

[EntitySystemOf(typeof(BattleLethalTempoComponent))]
public static class BattleLethalTempoComponentSystem
{
    public static bool HasValidTarget(this BattleLethalTempoComponent self)
    {
        var unit = self?.GetParent<BattleUnit>();
        var target = self?.Target;
        var battle = unit?.GetParent<BattleEntity>();
        return self != null && self.IsActive && unit != null && unit.CanAct() && target != null &&
            target.IsActive && target.IsAlive && target.Identity != null && target.Position != null && target.Combat != null &&
            target.Side != unit.Side && target.GetParent<BattleEntity>() == battle && battle.GetUnitAt(target.SlotIndex) == target;
    }

    public static int HitCount(this BattleLethalTempoComponent self, int round)
    {
        if (self == null || !self.HasValidTarget()) return 1;
        return (int)Math.Min(int.MaxValue, (long)self.InitialHits + (long)Math.Max(0, round - self.FirstRound) * self.ExtraHitsPerRound);
    }

    public static BattleUnit RetainedTargetInRange(this BattleLethalTempoComponent self)
    {
        if (self == null) return null;
        var unit = self.GetParent<BattleUnit>();
        if (!self.HasValidTarget())
        {
            self.Dispose();
            return null;
        }
        if (BattleMovementComponentSystem.GridDistance(unit.SlotIndex, self.Target.SlotIndex,
            unit.GetParent<BattleEntity>().Battlefield.RowCount) <= unit.AttackRange) return self.Target;
        self.Dispose();
        return null;
    }
}
