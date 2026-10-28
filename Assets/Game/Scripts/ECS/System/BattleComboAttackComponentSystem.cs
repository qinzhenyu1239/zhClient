using Framework.ECS;

[EntitySystemOf(typeof(BattleComboAttackComponent))]
public static class BattleComboAttackComponentSystem
{
    public static int HitCount(this BattleComboAttackComponent self, BattleRoundComponent round)
    {
        if (self == null || self.IsDisposed || !self.IsActive || round.RoundNumber < self.FirstRound || round.RoundNumber > self.LastRound) return 1;
        return round.Random.Next(self.MinHits, self.MaxHits + 1);
    }

    public static void OnRoundStarted(this BattleComboAttackComponent self, int roundNumber)
    {
        if (self != null && !self.IsDisposed && roundNumber > self.LastRound) self.Dispose();
    }
}
