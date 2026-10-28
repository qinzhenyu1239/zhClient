using Framework.ECS;

[EntitySystemOf(typeof(BattleBasicAttackComponent))]
public static class BattleBasicAttackComponentSystem
{
    public static BattleUnit FindTargetInRange(this BattleBasicAttackComponent self)
    {
        BattleUnit unit = self?.GetParent<BattleUnit>();
        return self != null && !self.IsDisposed && self.IsActive && unit?.Combat != null
            ? unit.Combat.FindTargetInRange(unit.AttackRange) : null;
    }

    public static void TryAttack(this BattleBasicAttackComponent self, BattleActionResult result)
    {
        BattleUnit attacker = self?.GetParent<BattleUnit>();
        BattleRoundComponent round = attacker?.GetParent<BattleEntity>()?.Round;
        if (round == null || attacker.Action == null || attacker.Action.HasActedThisRound) return;
        BattleUnit target = self.FindTargetInRange();
        if (target == null) return;
        int hits = attacker.GetComponent<BattleComboAttackComponent>().HitCount(round);
        for (int i = 0; i < hits && target.IsAlive; i++) attacker.Combat.ResolveDirectHit(target, result);
    }
}
