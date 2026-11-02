using Framework.ECS;

[EntitySystemOf(typeof(BattlePrecisionComponent))]
public static class BattlePrecisionComponentSystem
{
    public static bool HasPrecision(this BattleUnit unit)
    {
        var buff = unit?.GetComponent<BattlePrecisionComponent>();
        var round = unit?.GetParent<BattleEntity>()?.Round;
        return unit != null && unit.IsAlive && buff != null && buff.IsActive && round != null &&
            round.RoundNumber >= buff.FirstRound && round.RoundNumber <= buff.LastRound;
    }

    public static void OnRoundStarted(this BattlePrecisionComponent self, int round)
    {
        if (self != null && !self.IsDisposed && round > self.LastRound) self.Dispose();
    }
}
