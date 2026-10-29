using Framework.ECS;

[EntitySystemOf(typeof(BattleComboAttackComponent))]
public static class BattleComboAttackComponentSystem
{
    public static bool IsReady(this BattleComboAttackComponent self, BattleRoundComponent round) =>
        self != null && !self.IsDisposed && self.IsActive && round != null &&
        round.RoundNumber >= self.FirstRound && self.RemainingAttacks > 0;

    public static int HitCount(this BattleComboAttackComponent self, BattleRoundComponent round)
    {
        if (!self.IsReady(round)) return 1;
        return round.Random.Next(self.MinHits, self.MaxHits + 1);
    }

    public static void ConsumeAttack(this BattleComboAttackComponent self, BattleRoundComponent round)
    {
        if (!self.IsReady(round)) return;
        self.RemainingAttacks--;
        if (self.RemainingAttacks == 0) self.Dispose();
    }
}
