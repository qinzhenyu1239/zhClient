using Framework.ECS;

[EntitySystemOf(typeof(BattleSilenceComponent))]
public static class BattleSilenceComponentSystem
{
    public static bool IsSilenced(this BattleUnit unit)
    {
        var silence = unit?.GetComponent<BattleSilenceComponent>();
        var round = unit?.GetParent<BattleEntity>()?.Round;
        return silence != null && silence.IsActive && round != null &&
            round.RoundNumber >= silence.FirstRound && round.RoundNumber <= silence.LastRound;
    }

    public static void OnRoundStarted(this BattleSilenceComponent self, int round)
    {
        if (self != null && !self.IsDisposed && round > self.LastRound) self.Dispose();
    }
}
