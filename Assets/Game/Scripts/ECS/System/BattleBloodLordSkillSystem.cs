using System;
using System.Collections.Generic;
using System.Linq;
using Framework.ECS;

[EntitySystemOf(typeof(BattleSkillComponent))]
public static class BattleBloodLordSkillSystem
{
    public static List<int> BloodLeapDestinations(this BattleUnit unit, BattleUnit target)
    {
        var field = unit?.GetParent<BattleEntity>()?.Battlefield;
        if (field == null || target == null || !target.IsActive || !target.IsAlive || target.Position == null ||
            target.GetParent<BattleEntity>() != unit.GetParent<BattleEntity>()) return new List<int>();
        return Enumerable.Range(0, field.SlotCount).Where(slot => unit.CanEnterSlot(slot) && field.GetUnitAt(slot) == null &&
            BattleMovementComponentSystem.GridDistance(slot, target.SlotIndex, field.RowCount) == 1)
            .OrderBy(slot => BattleMovementComponentSystem.GridDistance(unit.SlotIndex, slot, field.RowCount)).ThenBy(slot => slot).ToList();
    }

    public static BattleUnit FindBloodLeapTarget(this BattleUnit unit, float range)
    {
        return unit.Combat.FindEnemies(range, int.MaxValue).Where(target => unit.BloodLeapDestinations(target).Count > 0)
            .OrderBy(target => target.CurrentHp)
            .ThenBy(target => BattleMovementComponentSystem.GridDistance(unit.SlotIndex, target.SlotIndex, unit.GetParent<BattleEntity>().Battlefield.RowCount))
            .ThenBy(target => target.SlotIndex).FirstOrDefault();
    }

    public static List<BattleUnit> BloodDrainTargets(this BattleUnit unit, float range, int count)
    {
        var field = unit?.GetParent<BattleEntity>()?.Battlefield;
        if (unit == null || !unit.CanAct() || field == null) return new List<BattleUnit>();
        return field.Units.Where(target => target != unit && target.IsActive && target.IsAlive && target.Identity != null &&
            target.Position != null && target.Combat != null && field.GetUnitAt(target.SlotIndex) == target &&
            Math.Max(Math.Abs(unit.SlotIndex / field.RowCount - target.SlotIndex / field.RowCount),
                Math.Abs(unit.SlotIndex % field.RowCount - target.SlotIndex % field.RowCount)) <= range)
            .OrderByDescending(target => target.CurrentHp).ThenBy(target => target.SlotIndex).Take(count).ToList();
    }

    public static bool CanUseBloodLordSkill(this BattleUnit unit, BattleSkillDefinition definition)
    {
        if (definition.Id == BattleSkillDefinition.JumpingCleave) return unit.FindBloodLeapTarget(definition.Range) != null;
        if (definition.Id == BattleSkillDefinition.AbsoluteDominion)
            return unit.BloodDrainTargets(definition.Range, definition.Int(1)).Count > 0;
        return true;
    }

    public static void CastBloodLordSkill(this BattleSkillComponent self, BattleSkillDefinition definition, BattleActionResult action)
    {
        var unit = self.GetParent<BattleUnit>();
        var battle = unit.GetParent<BattleEntity>();
        switch (definition.Id)
        {
            case BattleSkillDefinition.JumpingCleave:
                var target = unit.FindBloodLeapTarget(definition.Range);
                if (target == null) return;
                int slot = unit.BloodLeapDestinations(target).First();
                if (!unit.Position.TryPlace(slot)) return;
                // The requested multiplier applies to ordinary attack damage after defense.
                unit.Combat.ResolveHit(target, action, Math.Max(0, unit.Attack - target.Defense) * definition.Percent(1));
                action.ToSlot = unit.SlotIndex;
                action.Message = "跳跃重击";
                break;
            case BattleSkillDefinition.AbsoluteDominion:
                var victims = unit.BloodDrainTargets(definition.Range, definition.Int(1))
                    .Select(victim => new { Unit = victim, Slot = victim.SlotIndex, Amount = victim.CurrentHp * definition.Percent(2) }).ToList();
                float drained = 0;
                foreach (var victim in victims)
                {
                    if (!unit.IsAlive) break;
                    if (!victim.Unit.IsActive || !victim.Unit.IsAlive) continue;
                    float damage = victim.Unit.Health.DrainHealth(victim.Amount);
                    drained += damage;
                    if (victim.Unit.Side != unit.Side) unit.RecordDamage(damage);
                    action.Target = victim.Unit;
                    action.TargetSlot = victim.Slot;
                    action.Hit = true;
                    action.Damage += damage;
                    action.Hits.Add(new BattleHitResult { TargetSlot = victim.Slot, Hit = true, Damage = damage });
                }
                action.Healing += unit.Health.Heal(drained);
                action.ToSlot = unit.SlotIndex;
                action.Message = "绝对统治 · 吸收周围敌我英雄当前血量";
                break;
            case BattleSkillDefinition.BloodDomain:
                var domain = unit.GetComponent<BattleBloodDomainComponent>() ?? unit.AddComponent<BattleBloodDomainComponent>();
                domain.FirstRound = battle.Round.RoundNumber;
                domain.LastRound = battle.Round.RoundNumber + definition.Int(1) - 1;
                domain.AttackRatio = definition.Percent(2);
                domain.DefenseRatio = definition.Percent(3);
                domain.HealingRatio = definition.Percent(4);
                domain.DefenseCap = definition.Int(5);
                action.Message = "血裔领域 · 本回合及下一回合吸收阵亡单位";
                break;
        }
    }
}
