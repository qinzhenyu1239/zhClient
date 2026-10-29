using Framework.ECS;

[EntitySystemOf(typeof(BattleJudgmentComponent))]
public static class BattleJudgmentComponentSystem
{
    public static bool BelowJudgmentThreshold(this BattleUnit target, float percent)
    {
        // Compare in percentage units: exact 15% must survive, including MaxHp=1400, Hp=210.
        return target != null && target.IsAlive && target.MaxHp > 0 &&
            (double)target.CurrentHp * 100d < (double)target.MaxHp * percent;
    }

    public static bool ExecuteJudgment(this BattleUnit source, BattleUnit target, float threshold, BattleActionResult action)
    {
        var battle = source?.GetParent<BattleEntity>();
        if (source == null || !source.IsActive || !source.IsAlive || source.Identity == null || source.Position == null ||
            target == null || !target.IsActive || target.Identity == null || target.Position == null || target.Side == source.Side ||
            target.GetParent<BattleEntity>() != battle || battle?.Round == null || battle.Round.Phase == BattlePhase.Finished ||
            battle.GetUnitAt(source.SlotIndex) != source || battle.GetUnitAt(target.SlotIndex) != target || !target.BelowJudgmentThreshold(threshold)) return false;
        int slot = target.SlotIndex;
        float damage = target.Health.Execute();
        if (damage <= 0) return false;
        source.RecordDamage(damage);
        action.Target = target;
        action.TargetSlot = slot;
        action.Hit = true;
        action.Damage += damage;
        if (action.Hits.Count > 0 && action.Hits[action.Hits.Count - 1].TargetSlot == slot)
        {
            var hit = action.Hits[action.Hits.Count - 1];
            hit.Damage += damage;
            hit.Executed = true;
        }
        else action.Hits.Add(new BattleHitResult { TargetSlot = slot, Damage = damage, Hit = true, Executed = true });
        return true;
    }

    public static bool CheckJudgments(this BattleEntity battle)
    {
        if (battle?.Round == null || battle.Battlefield == null || battle.Round.Phase == BattlePhase.Finished || !battle.IsActive) return false;
        bool executed = false;
        foreach (var source in battle.Battlefield.Units)
        {
            var judgment = source.GetComponent<BattleJudgmentComponent>();
            if (judgment == null || !judgment.IsActive || !source.IsAlive ||
                battle.Round.RoundNumber < judgment.FirstRound || battle.Round.RoundNumber > judgment.LastRound) continue;
            foreach (var target in battle.Battlefield.Units)
            {
                if (target.Side == source.Side || !target.BelowJudgmentThreshold(judgment.ThresholdPercent)) continue;
                var action = new BattleActionResult
                {
                    Kind = BattleActionKind.Execution, SkillId = BattleSkillDefinition.HolyJudgment,
                    Unit = source, FromSlot = source.SlotIndex, ToSlot = source.SlotIndex,
                    RoundNumber = battle.Round.RoundNumber, Message = "神圣裁决 · 斩杀"
                };
                if (!source.ExecuteJudgment(target, judgment.ThresholdPercent, action)) continue;
                battle.Round.PendingReactions.Enqueue(action);
                executed = true;
            }
        }
        return executed;
    }

    public static void OnRoundStarted(this BattleJudgmentComponent self, int round)
    {
        if (self != null && !self.IsDisposed && round > self.LastRound) self.Dispose();
    }
}
