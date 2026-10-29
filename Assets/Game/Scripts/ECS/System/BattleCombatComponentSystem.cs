using System;
using System.Collections.Generic;
using System.Linq;
using Framework.ECS;

[EntitySystemOf(typeof(BattleCombatComponent))]
public static class BattleCombatComponentSystem
{
    public static BattleUnit FindTargetInRange(this BattleCombatComponent self, float range)
    {
        BattleUnit attacker = self?.GetParent<BattleUnit>();
        BattlefieldComponent field = attacker?.GetParent<BattleEntity>()?.Battlefield;
        if (self == null || self.IsDisposed || attacker == null || !attacker.IsAlive || attacker.Identity == null ||
            attacker.Position == null || field == null || field.GetUnitAt(attacker.SlotIndex) != attacker) return null;
        BattleUnit adjacent = range >= 1 ? attacker.FindAdjacentEnemy() : null;
        if (adjacent != null) return adjacent;
        BattleUnit nearest = null;
        int nearestDistance = int.MaxValue;
        foreach (BattleUnit target in field.Units)
        {
            if (!target.IsAlive || target.Identity == null || target.Position == null || target.Combat == null ||
                target.Side == attacker.Side || field.GetUnitAt(target.SlotIndex) != target) continue;
            int distance = Math.Abs(attacker.SlotIndex / field.RowCount - target.SlotIndex / field.RowCount) +
                Math.Abs(attacker.SlotIndex % field.RowCount - target.SlotIndex % field.RowCount);
            if (distance <= range && distance < nearestDistance) { nearest = target; nearestDistance = distance; }
        }
        return nearest;
    }

    public static List<BattleUnit> FindEnemies(this BattleCombatComponent self, float range, int count)
    {
        BattleUnit unit = self?.GetParent<BattleUnit>();
        var field = unit?.GetParent<BattleEntity>()?.Battlefield;
        if (self == null || !self.IsActive || unit == null || !unit.IsAlive || unit.Position == null || field == null)
            return new List<BattleUnit>();
        return field.Units.Where(target => target.IsActive && target.IsAlive && target.Identity != null &&
            target.Position != null && target.Combat != null && target.Side != unit.Side && field.GetUnitAt(target.SlotIndex) == target)
            .Select(target => new { Unit = target, Distance = Math.Abs(unit.SlotIndex / field.RowCount - target.SlotIndex / field.RowCount) +
                Math.Abs(unit.SlotIndex % field.RowCount - target.SlotIndex % field.RowCount) })
            .Where(target => target.Distance <= range).OrderBy(target => target.Distance).ThenBy(target => target.Unit.SlotIndex)
            .Take(count).Select(target => target.Unit).ToList();
    }

    public static void ResolveDirectHit(this BattleCombatComponent self, BattleUnit target, BattleActionResult action, float attackMultiplier = 1)
    {
        if (self == null || target == null) return;
        self.ResolveHit(target, action, Math.Max(0, self.Attack * attackMultiplier - target.Defense));
    }

    public static void ResolveHit(this BattleCombatComponent self, BattleUnit target, BattleActionResult action, float damageAmount, bool guaranteed = false)
    {
        BattleUnit attacker = self?.GetParent<BattleUnit>();
        BattleRoundComponent round = attacker?.GetParent<BattleEntity>()?.Round;
        if (self == null || self.IsDisposed || round == null || target == null || !target.IsAlive || target.Combat == null || target.Position == null) return;
        action.Target = target;
        action.TargetSlot = target.SlotIndex;
        double chance = self.HitChance(target.Combat);
        bool hit = guaranteed || chance >= 1d || (chance > 0d && round.Random.NextDouble() < chance);
        float absorbed = 0;
        float damage = hit ? target.Health.ApplyDamage(damageAmount, out absorbed) : 0;
        float healing = hit ? attacker.GetComponent<BattleLifestealComponent>().HealFromDirectDamage(damage) : 0;
        action.Hit |= hit;
        action.Damage += damage;
        action.Healing += healing;
        action.Hits.Add(new BattleHitResult { TargetSlot = target.SlotIndex, Hit = hit, Damage = damage, Healing = healing, ShieldDamage = absorbed });
    }

    public static double HitChance(float attackerSpeed, float defenderSpeed)
    {
        return Math.Max(0d, Math.Min(1d, 1d - ((double)defenderSpeed - attackerSpeed) / 100d));
    }

    public static float BasicDamage(int attack, int defense)
    {
        return (float)Math.Max(0d, (double)attack - defense);
    }

    public static double HitChance(this BattleCombatComponent self, BattleCombatComponent target) => HitChance(self.Speed, target.Speed);
    public static float BasicDamage(this BattleCombatComponent self, BattleCombatComponent target) => BasicDamage(self.Attack, target.Defense);
}
