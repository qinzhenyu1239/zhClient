using System;
using Framework.ECS;

[EntitySystemOf(typeof(BattleSkillPointComponent))]
public static class BattleSkillPointComponentSystem
{
    [EntitySystem]
    private static void Awake(this BattleSkillPointComponent self)
    {
        self.PointsPerRound = 30;
        self.BeginRound(self.GetParent<BattleEntity>()?.Round?.RoundNumber ?? 1);
    }

    public static void BeginRound(this BattleSkillPointComponent self, int roundNumber)
    {
        if (self == null || self.IsDisposed) return;
        self.RoundNumber = roundNumber;
        self.Current = self.PointsPerRound;
        self.EnemyCurrent = self.PointsPerRound;
    }

    public static int CurrentFor(this BattleSkillPointComponent self, BattleSide side)
    {
        return self == null || self.IsDisposed ? 0 : side == BattleSide.Enemy ? self.EnemyCurrent : self.Current;
    }

    public static bool TrySpend(this BattleSkillPointComponent self, int amount, BattleSide side = BattleSide.Ally)
    {
        if (self == null || self.IsDisposed || amount < 0 || amount > self.CurrentFor(side)) return false;
        if (side == BattleSide.Enemy) self.EnemyCurrent -= amount;
        else self.Current -= amount;
        return true;
    }

    public static void Refund(this BattleSkillPointComponent self, int amount, int reservedRound, BattleSide side = BattleSide.Ally)
    {
        if (self == null || self.IsDisposed || self.RoundNumber != reservedRound || amount <= 0) return;
        int refunded = Math.Min(self.PointsPerRound, self.CurrentFor(side) + amount);
        if (side == BattleSide.Enemy) self.EnemyCurrent = refunded;
        else self.Current = refunded;
    }
}
