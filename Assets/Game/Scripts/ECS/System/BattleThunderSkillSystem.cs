using System;
using Framework.ECS;

[EntitySystemOf(typeof(BattleSkillComponent))]
public static class BattleThunderSkillSystem
{
    public static void CastThunderSkill(this BattleSkillComponent self, BattleSkillDefinition definition, BattleUnit target, BattleActionResult action)
    {
        var unit = self.GetParent<BattleUnit>();
        var round = unit.GetParent<BattleEntity>().Round;
        switch (definition.Id)
        {
            case BattleSkillDefinition.ThunderStrike:
                unit.Combat.ResolveDirectHit(target, action, definition.Percent(1));
                if (action.Hit && target.IsAlive)
                {
                    var silence = target.GetComponent<BattleSilenceComponent>() ?? target.AddComponent<BattleSilenceComponent>();
                    int first = round.RoundNumber + 1;
                    silence.FirstRound = silence.FirstRound > 0 ? Math.Min(silence.FirstRound, first) : first;
                    silence.LastRound = Math.Max(silence.LastRound, round.RoundNumber + definition.Int(2));
                }
                action.Message = "雷霆重击 · 沉默下一回合";
                break;
            case BattleSkillDefinition.ChainLightning:
                foreach (var enemy in unit.Combat.FindEnemies(definition.Range, definition.Int(1)))
                    unit.Combat.ResolveHit(enemy, action, enemy.MaxHp * definition.Percent(2));
                unit.AddShield(action.Damage, definition.Int(3));
                action.ShieldGained = action.Damage;
                action.Message = "连锁闪电 · 伤害转化护盾";
                break;
            case BattleSkillDefinition.Thundercloud:
                var cloud = unit.GetComponent<BattleThundercloudComponent>() ?? unit.AddComponent<BattleThundercloudComponent>();
                cloud.RemainingStrikes = definition.Int(1);
                cloud.SecondsUntilStrike = 1;
                cloud.AttackMultiplier = definition.Percent(2);
                cloud.KillShieldMultiplier = definition.Percent(3);
                cloud.ShieldRounds = definition.Int(4);
                cloud.Range = definition.Range;
                action.Message = "召唤闪电雷云 · 每秒轰击一次，共" + cloud.RemainingStrikes + "次";
                break;
        }
    }
}
