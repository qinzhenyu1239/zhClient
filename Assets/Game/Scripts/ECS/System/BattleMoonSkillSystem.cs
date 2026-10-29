using System;
using System.Collections.Generic;
using System.Linq;
using Framework.ECS;

[EntitySystemOf(typeof(BattleSkillComponent))]
public static class BattleMoonSkillSystem
{
    public static List<BattleUnit> MoonRowTargets(this BattleUnit unit)
    {
        var field = unit.GetParent<BattleEntity>().Battlefield;
        return unit.Combat.FindEnemies(float.MaxValue, int.MaxValue)
            .Where(enemy => enemy.SlotIndex % field.RowCount == unit.SlotIndex % field.RowCount).ToList();
    }

    public static List<int> EmptyMoonSlots(this BattleUnit unit)
    {
        var field = unit.GetParent<BattleEntity>().Battlefield;
        return Enumerable.Range(0, field.SlotCount).Where(slot => unit.CanEnterSlot(slot) && field.GetUnitAt(slot) == null).ToList();
    }

    public static bool CanUseMoonSkill(this BattleUnit unit, int id)
    {
        if (id == BattleSkillDefinition.MoonCleave) return unit.MoonRowTargets().Count > 0;
        if (id == BattleSkillDefinition.MoonBlink) return unit.EmptyMoonSlots().Count > 0;
        if (id == BattleSkillDefinition.MoonShower)
            return unit.GetComponent<BattleMoonShowerComponent>() == null && unit.Combat.FindEnemies(float.MaxValue, 1).Count > 0;
        return true;
    }

    public static void ApplyMoonMark(this BattleUnit source, BattleUnit target, BattleActionResult action)
    {
        if (!source.CanAct() || target == null || !target.IsActive || !target.IsAlive || target.Combat == null ||
            target.Side == source.Side || target.GetParent<BattleEntity>() != source.GetParent<BattleEntity>()) return;
        var definition = source.Skills.GetDefinition(BattleSkillDefinition.MoonShower);
        int threshold = definition?.Int(3) ?? 2;
        float multiplier = definition?.Percent(4) ?? 2;
        var mark = target.GetComponent<BattleMoonMarkComponent>() ?? target.AddComponent<BattleMoonMarkComponent>();
        mark.Stacks++;
        action.MoonMarkedSlots.Add(target.SlotIndex);
        if (mark.Stacks < threshold) return;
        mark.Dispose();
        // Detonation is guaranteed damage; the normal direct-hit armor/shield/lifesteal rules still apply.
        source.Combat.ResolveHit(target, action, Math.Max(0, source.Attack * multiplier - target.Defense), true);
        action.MoonExplosions++;
    }

    public static void CastMoonSkill(this BattleSkillComponent self, BattleSkillDefinition definition, BattleActionResult action)
    {
        BattleUnit unit = self.GetParent<BattleUnit>();
        var battle = unit.GetParent<BattleEntity>();
        if (definition.Id == BattleSkillDefinition.MoonShower)
        {
            var shower = unit.AddComponent<BattleMoonShowerComponent>();
            shower.Interval = definition.Value(2);
            shower.SecondsUntilMark = shower.Interval;
            shower.RemainingMarks = definition.MoonTickCount();
            shower.MaxMarksPerEnemy = definition.Int(5);
            action.Message = "月华倾泻 · 持续" + definition.Value(1) + "秒";
            return;
        }
        List<BattleUnit> targets;
        if (definition.Id == BattleSkillDefinition.MoonBlink)
        {
            var empty = unit.EmptyMoonSlots();
            unit.Position.TryPlace(empty[battle.Round.Random.Next(empty.Count)]);
            action.ToSlot = unit.SlotIndex;
            targets = unit.Combat.FindEnemies(1, int.MaxValue);
            action.Message = "月影瞬杀 · 随机瞬移并攻击相邻敌人";
        }
        else
        {
            targets = unit.MoonRowTargets();
            action.Message = "月刃横扫 · 攻击同一行所有敌人";
        }
        foreach (BattleUnit target in targets)
        {
            int before = action.Hits.Count;
            unit.Combat.ResolveDirectHit(target, action, definition.Percent(1));
            if (action.Hits.Count > before && action.Hits[action.Hits.Count - 1].Hit)
                unit.ApplyMoonMark(target, action);
        }
    }
}
