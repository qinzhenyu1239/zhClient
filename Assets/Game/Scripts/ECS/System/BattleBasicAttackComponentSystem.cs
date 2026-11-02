using Framework.ECS;

[EntitySystemOf(typeof(BattleBasicAttackComponent))]
public static class BattleBasicAttackComponentSystem
{
    public static BattleUnit FindTargetInRange(this BattleBasicAttackComponent self)
    {
        BattleUnit unit = self?.GetParent<BattleUnit>();
        if (self == null || self.IsDisposed || !self.IsActive || unit?.Combat == null) return null;
        var tempo = unit.GetComponent<BattleLethalTempoComponent>();
        BattleUnit retained = tempo.RetainedTargetInRange();
        if (retained != null) return retained;
        BattleUnit target = unit.Combat.FindTargetInRange(unit.AttackRange);
        if (target != null && tempo != null && !tempo.IsDisposed && tempo.Target != target) tempo.Dispose();
        return target;
    }

    public static void TryAttack(this BattleBasicAttackComponent self, BattleActionResult result)
    {
        BattleUnit attacker = self?.GetParent<BattleUnit>();
        BattleRoundComponent round = attacker?.GetParent<BattleEntity>()?.Round;
        if (round == null || attacker.Action == null || attacker.Action.HasActedThisRound) return;
        if (attacker.IsSilenced()) { result.Message = "沉默中，无法普攻或释放技能"; return; }
        BattleUnit target = self.FindTargetInRange();
        if (target == null) return;
        if (attacker.CharacterId == BattleEntity.JungleHunterId)
        {
            var tempo = attacker.GetComponent<BattleLethalTempoComponent>();
            int arrowCount = tempo.HitCount(round.RoundNumber);
            attacker.FireHunterArrows(target, arrowCount, result);
            if (arrowCount > 1) result.Message = "致命节奏 · 连射 " + arrowCount + " 箭";
            return;
        }
        var combo = attacker.GetComponent<BattleComboAttackComponent>();
        int hits = combo.HitCount(round);
        for (int i = 0; i < hits && target.IsAlive; i++) attacker.Combat.ResolveDirectHit(target, result);
        combo.ConsumeAttack(round);
    }
}
