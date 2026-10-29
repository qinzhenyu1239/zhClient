using System.Collections.Generic;
using Framework.ECS;

[EntitySystemOf(typeof(BattleEnemyAIComponent))]
public static class BattleEnemyAIComponentSystem
{
    public static void PlanSkills(this BattleEnemyAIComponent self)
    {
        var battle = self?.GetParent<BattleEntity>();
        if (self == null || !self.IsActive || battle?.Battlefield == null || battle.Round == null || battle.Round.Phase == BattlePhase.Finished) return;
        foreach (var unit in battle.Battlefield.Units) self.TrySelectSkill(unit);
    }

    public static void TrySelectSkill(this BattleEnemyAIComponent self, BattleUnit unit)
    {
        var battle = self?.GetParent<BattleEntity>();
        if (self == null || !self.IsActive || unit == null || unit.Side != BattleSide.Enemy ||
            unit.GetParent<BattleEntity>() != battle || !unit.CanAct() || unit.Skills == null ||
            unit.Skills.PendingSkillId != 0 || battle?.Round == null || unit.IsSilenced()) return;
        var usable = new List<int>();
        foreach (int id in new[] { unit.Skill1Id, unit.Skill2Id, unit.Skill3Id })
        {
            if (usable.Contains(id) || !unit.Skills.CanQueue(id, out _)) continue;
            var definition = unit.Skills.GetDefinition(id);
            if (id == BattleSkillDefinition.DoubleStrike || id == BattleSkillDefinition.ThunderStrike ||
                id == BattleSkillDefinition.ChainLightning || id == BattleSkillDefinition.Thundercloud)
            {
                if (unit.Combat.FindTargetInRange(definition.Range) == null) continue;
            }
            if (id == BattleSkillDefinition.ComboAttack &&
                unit.GetComponent<BattleComboAttackComponent>()?.LastRound > battle.Round.RoundNumber) continue;
            if (id == BattleSkillDefinition.Thundercloud &&
                unit.GetComponent<BattleThundercloudComponent>()?.RemainingStrikes > 0) continue;
            usable.Add(id);
        }
        if (usable.Count > 0) unit.Skills.TryQueue(usable[battle.Round.Random.Next(usable.Count)], out _);
    }
}
