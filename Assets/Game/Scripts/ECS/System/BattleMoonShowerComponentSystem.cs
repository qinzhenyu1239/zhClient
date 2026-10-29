using System;
using Framework.ECS;

[EntitySystemOf(typeof(BattleMoonShowerComponent))]
public static class BattleMoonShowerComponentSystem
{
    public static double NextMoonEvent(this BattleEntity battle)
    {
        double next = double.PositiveInfinity;
        foreach (BattleUnit unit in battle.Battlefield.Units)
        {
            var shower = unit.GetComponent<BattleMoonShowerComponent>();
            if (shower != null && shower.IsActive && unit.CanAct() && unit.Combat != null)
                next = Math.Min(next, Math.Max(0, shower.SecondsUntilMark));
        }
        return next;
    }

    public static void AdvanceMoonShowers(this BattleEntity battle, double elapsed)
    {
        foreach (BattleUnit unit in battle.Battlefield.Units)
        {
            var shower = unit.GetComponent<BattleMoonShowerComponent>();
            if (shower == null) continue;
            if (!unit.CanAct() || unit.Combat == null) { shower.Dispose(); continue; }
            if (!shower.IsActive) continue;
            shower.SecondsUntilMark -= elapsed;
            if (shower.SecondsUntilMark > 0.0000001) continue;
            var targets = unit.Combat.FindEnemies(float.MaxValue, int.MaxValue);
            targets.RemoveAll(target => shower.AppliedMarks.TryGetValue(target, out int count) && count >= shower.MaxMarksPerEnemy);
            if (targets.Count == 0) { shower.Dispose(); continue; }
            var action = new BattleActionResult
            {
                Kind = BattleActionKind.Moonlight, SkillId = BattleSkillDefinition.MoonShower,
                RoundNumber = battle.Round.RoundNumber, Unit = unit, FromSlot = unit.SlotIndex,
                ToSlot = unit.SlotIndex, Message = "月光印记"
            };
            BattleUnit target = targets[battle.Round.Random.Next(targets.Count)];
            unit.ApplyMoonMark(target, action);
            shower.AppliedMarks.TryGetValue(target, out int applied);
            shower.AppliedMarks[target] = applied + 1;
            if (action.MoonExplosions > 0) action.Message = "月光印记 · 双层引爆";
            battle.Round.EnqueueAction(action);
            shower.RemainingMarks--;
            shower.SecondsUntilMark = shower.Interval;
            if (shower.RemainingMarks <= 0) shower.Dispose();
            if (battle.Battlefield.ResolveVictory()) return;
        }
    }
}
