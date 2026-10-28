using Framework.ECS;

[EntitySystemOf(typeof(BattleBleedComponent))]
public static class BattleBleedComponentSystem
{
    public static void OnRoundStarted(this BattleBleedComponent self, BattleRoundComponent round)
    {
        if (self == null || self.IsDisposed || !self.IsActive || round.RoundNumber < self.NextTickRound) return;
        BattleUnit target = self.GetParent<BattleUnit>();
        if (target == null || !target.IsAlive || target.Position == null || self.RemainingTicks <= 0)
        {
            self.Dispose();
            return;
        }
        int slot = target.SlotIndex;
        float damage = target.Health.ApplyDamage(target.MaxHp * self.MaxHpRatio);
        var action = new BattleActionResult
        {
            RoundNumber = round.RoundNumber, Unit = target, Target = target,
            FromSlot = slot, ToSlot = slot, TargetSlot = slot, Hit = true, Damage = damage,
            Kind = BattleActionKind.Bleed, Message = "流血"
        };
        action.Hits.Add(new BattleHitResult { TargetSlot = slot, Hit = true, Damage = damage });
        round.PendingActions.Enqueue(action);
        self.RemainingTicks--;
        self.NextTickRound = round.RoundNumber + 1;
        if (self.RemainingTicks <= 0 || !target.IsAlive) self.Dispose();
    }
}
