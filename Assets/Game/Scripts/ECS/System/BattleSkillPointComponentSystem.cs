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
    }

    public static bool TrySpend(this BattleSkillPointComponent self, int amount)
    {
        if (self == null || self.IsDisposed || amount < 0 || amount > self.Current) return false;
        self.Current -= amount;
        return true;
    }

    public static void Refund(this BattleSkillPointComponent self, int amount, int reservedRound)
    {
        if (self == null || self.IsDisposed || self.RoundNumber != reservedRound || amount <= 0) return;
        self.Current = Math.Min(self.PointsPerRound, self.Current + amount);
    }
}
