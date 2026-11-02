using System;
using System.Linq;
using Framework.ECS;

[EntitySystemOf(typeof(BattleSkillComponent))]
public static class BattleHunterSkillSystem
{
    public static int FindSkirmishSlot(this BattleUnit unit, int maxDistance)
    {
        var field = unit?.GetParent<BattleEntity>()?.Battlefield;
        if (unit == null || !unit.CanAct() || field == null) return -1;
        var enemies = unit.Combat.FindEnemies(float.MaxValue, int.MaxValue);
        if (enemies.Count == 0) return -1;
        // Keep an enemy in basic-attack range so that the displacement can be followed by both arrows.
        return Enumerable.Range(0, field.SlotCount)
            .Where(slot => unit.CanEnterSlot(slot) && (field.GetUnitAt(slot) == null || field.GetUnitAt(slot) == unit))
            .Select(slot => new { Slot = slot,
                Travel = BattleMovementComponentSystem.GridDistance(unit.SlotIndex, slot, field.RowCount),
                NearestEnemy = enemies.Min(enemy => BattleMovementComponentSystem.GridDistance(slot, enemy.SlotIndex, field.RowCount)) })
            .Where(candidate => candidate.Travel <= maxDistance && candidate.NearestEnemy <= unit.AttackRange)
            .OrderByDescending(candidate => candidate.NearestEnemy).ThenBy(candidate => candidate.Travel).ThenBy(candidate => candidate.Slot)
            .Select(candidate => candidate.Slot).DefaultIfEmpty(-1).First();
    }

    public static bool CanUseHunterSkill(this BattleUnit unit, int id)
    {
        if (id == BattleSkillDefinition.JungleSkirmish)
            return unit.FindSkirmishSlot(unit.Skills.GetDefinition(id).Int(1)) >= 0;
        if (id == BattleSkillDefinition.LethalTempo)
            return unit.Combat.FindEnemies(unit.AttackRange, 1).Count > 0;
        return true;
    }

    public static void FireHunterArrows(this BattleUnit unit, BattleUnit target, int count, BattleActionResult action)
    {
        if (unit == null || !unit.CanAct() || target == null) return;
        var tempo = unit.GetComponent<BattleLethalTempoComponent>();
        if (tempo != null && (!tempo.HasValidTarget() || tempo.Target != target))
        {
            tempo.Dispose();
            tempo = null;
        }
        float ratio = tempo != null && tempo.IsActive ? tempo.DamageRatio : 1f;
        for (int i = 0; i < count && unit.CanAct() && target.IsAlive && target.IsActive; i++)
        {
            int before = action.Hits.Count;
            // Lethal Tempo halves normal attack damage after defense, before shields.
            unit.Combat.ResolveHit(target, action, Math.Max(0, unit.Attack - target.Defense) * ratio);
            if (action.Hits.Count > before) action.Hits[action.Hits.Count - 1].IsArrow = true;
        }
        if (tempo != null && !tempo.IsDisposed && !tempo.HasValidTarget()) tempo.Dispose();
    }

    public static void CastHunterSkill(this BattleSkillComponent self, BattleSkillDefinition definition, BattleActionResult action)
    {
        var unit = self.GetParent<BattleUnit>();
        var battle = unit.GetParent<BattleEntity>();
        switch (definition.Id)
        {
            case BattleSkillDefinition.JungleSkirmish:
                int slot = unit.FindSkirmishSlot(definition.Int(1));
                if (slot >= 0) unit.Position.TryPlace(slot);
                action.ToSlot = unit.SlotIndex;
                var target = unit.GetComponent<BattleBasicAttackComponent>().FindTargetInRange();
                unit.FireHunterArrows(target, definition.Int(2), action);
                action.Message = "丛林游射 · 位移后连射 " + definition.Int(2) + " 箭";
                break;
            case BattleSkillDefinition.PrecisionStrike:
                var allies = battle.Battlefield.Units.Where(ally => ally != unit && ally.IsActive && ally.IsAlive &&
                    ally.Identity != null && ally.Position != null && ally.Side == unit.Side && battle.GetUnitAt(ally.SlotIndex) == ally)
                    .OrderBy(ally => BattleMovementComponentSystem.GridDistance(unit.SlotIndex, ally.SlotIndex, battle.Battlefield.RowCount))
                    .ThenBy(ally => ally.SlotIndex).Take(definition.Int(1)).Concat(new[] { unit });
                foreach (var ally in allies)
                {
                    var precision = ally.GetComponent<BattlePrecisionComponent>() ?? ally.AddComponent<BattlePrecisionComponent>();
                    precision.FirstRound = Math.Min(precision.FirstRound == 0 ? battle.Round.RoundNumber : precision.FirstRound, battle.Round.RoundNumber);
                    precision.LastRound = Math.Max(precision.LastRound, battle.Round.RoundNumber + definition.Int(2) - 1);
                }
                action.Message = "精准打击 · 自身及最近友军必中";
                break;
            case BattleSkillDefinition.LethalTempo:
                var nearest = unit.Combat.FindEnemies(unit.AttackRange, 1).FirstOrDefault();
                if (nearest == null) return;
                var tempo = unit.GetComponent<BattleLethalTempoComponent>() ?? unit.AddComponent<BattleLethalTempoComponent>();
                tempo.Target = nearest;
                tempo.FirstRound = battle.Round.RoundNumber;
                tempo.InitialHits = definition.Int(1);
                tempo.ExtraHitsPerRound = definition.Int(2);
                tempo.DamageRatio = definition.Percent(3);
                unit.FireHunterArrows(nearest, tempo.InitialHits, action);
                action.Message = "致命节奏 · 锁定目标并连射";
                break;
        }
    }
}
