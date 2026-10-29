using System;
using Framework.ECS;

[EntitySystemOf(typeof(BattleThundercloudComponent))]
public static class BattleThundercloudComponentSystem
{
    public static double NextCloudEvent(this BattleEntity battle)
    {
        double next = double.PositiveInfinity;
        foreach (var unit in battle.Battlefield.Units)
        {
            var cloud = unit.GetComponent<BattleThundercloudComponent>();
            if (cloud != null && cloud.IsActive && unit.IsAlive)
                next = Math.Min(next, Math.Max(0, cloud.SecondsUntilStrike));
        }
        return next;
    }

    public static void AdvanceClouds(this BattleEntity battle, double elapsed)
    {
        foreach (var unit in battle.Battlefield.Units)
        {
            var cloud = unit.GetComponent<BattleThundercloudComponent>();
            if (cloud == null) continue;
            if (!unit.IsAlive || unit.Combat == null) { cloud.Dispose(); continue; }
            if (!cloud.IsActive) continue;
            cloud.SecondsUntilStrike -= elapsed;
            if (cloud.SecondsUntilStrike > 0.0000001) continue;
            var targets = unit.Combat.FindEnemies(cloud.Range, int.MaxValue);
            if (targets.Count == 0) { cloud.Dispose(); continue; }
            BattleUnit target = targets[battle.Round.Random.Next(targets.Count)];
            var action = new BattleActionResult
            {
                Kind = BattleActionKind.Lightning, SkillId = BattleSkillDefinition.Thundercloud,
                RoundNumber = battle.Round.RoundNumber, Unit = unit,
                FromSlot = unit.SlotIndex, ToSlot = unit.SlotIndex, Message = "闪电雷云"
            };
            unit.Combat.ResolveDirectHit(target, action, cloud.AttackMultiplier);
            if (!target.IsAlive)
            {
                float shield = unit.Attack * cloud.KillShieldMultiplier;
                unit.AddShield(shield, cloud.ShieldRounds);
                action.ShieldGained = shield;
            }
            battle.Round.PendingActions.Enqueue(action);
            cloud.RemainingStrikes--;
            cloud.SecondsUntilStrike = 1;
            if (cloud.RemainingStrikes <= 0) cloud.Dispose();
            if (battle.Battlefield.ResolveVictory()) return;
        }
    }
}
