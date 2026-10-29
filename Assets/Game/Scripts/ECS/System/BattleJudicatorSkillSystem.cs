using Framework.ECS;
using System.Linq;

[EntitySystemOf(typeof(BattleSkillComponent))]
public static class BattleJudicatorSkillSystem
{
    public static BattleUnit FindJudgmentBeamTarget(this BattleSkillComponent self, float range)
    {
        var unit = self?.GetParent<BattleUnit>();
        // FindEnemies already orders equal candidates by distance, then slot.
        return unit?.Combat.FindEnemies(range, int.MaxValue).OrderBy(target => target.CurrentHp).FirstOrDefault();
    }

    public static void CastJudicatorSkill(this BattleSkillComponent self, BattleSkillDefinition definition, BattleUnit target, BattleActionResult action)
    {
        var unit = self.GetParent<BattleUnit>();
        var battle = unit.GetParent<BattleEntity>();
        switch (definition.Id)
        {
            case BattleSkillDefinition.JudgmentBeam:
                unit.Combat.ResolveDirectHit(target, action, definition.Percent(1));
                if (action.Damage > 0) unit.ExecuteJudgment(target, definition.Value(2), action);
                action.Message = "裁决光束";
                break;
            case BattleSkillDefinition.HolyProtection:
                foreach (var ally in battle.Battlefield.Units)
                {
                    if (!ally.IsAlive || !ally.IsActive || ally.Identity == null || ally.Position == null ||
                        ally.Side != unit.Side || battle.GetUnitAt(ally.SlotIndex) != ally) continue;
                    float shield = unit.Attack * definition.Percent(1);
                    ally.AddPermanentShield(shield, definition.Int(2));
                    action.ShieldGained += shield;
                }
                action.Message = "神圣庇佑 · 全体护盾与防御加成";
                break;
            case BattleSkillDefinition.HolyJudgment:
                var judgment = unit.GetComponent<BattleJudgmentComponent>() ?? unit.AddComponent<BattleJudgmentComponent>();
                judgment.FirstRound = battle.Round.RoundNumber;
                judgment.LastRound = battle.Round.RoundNumber + definition.Int(2) - 1;
                judgment.ThresholdPercent = definition.Value(1);
                battle.CheckJudgments();
                action.Message = "神圣裁决 · 本回合及下一回合自动斩杀";
                break;
        }
    }
}
