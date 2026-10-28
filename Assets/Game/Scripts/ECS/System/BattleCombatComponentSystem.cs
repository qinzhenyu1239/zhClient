using System;
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

    public static void ResolveDirectHit(this BattleCombatComponent self, BattleUnit target, BattleActionResult action)
    {
        BattleUnit attacker = self?.GetParent<BattleUnit>();
        BattleRoundComponent round = attacker?.GetParent<BattleEntity>()?.Round;
        if (self == null || self.IsDisposed || round == null || target == null || !target.IsAlive || target.Combat == null || target.Position == null) return;
        action.Target = target;
        action.TargetSlot = target.SlotIndex;
        double chance = self.HitChance(target.Combat);
        bool hit = chance >= 1d || (chance > 0d && round.Random.NextDouble() < chance);
        float damage = hit ? target.Health.ApplyDamage(self.BasicDamage(target.Combat)) : 0;
        float healing = hit ? attacker.GetComponent<BattleLifestealComponent>().HealFromDirectDamage(damage) : 0;
        action.Hit |= hit;
        action.Damage += damage;
        action.Healing += healing;
        action.Hits.Add(new BattleHitResult { TargetSlot = target.SlotIndex, Hit = hit, Damage = damage, Healing = healing });
    }

    public static double HitChance(float attackerSpeed, float defenderSpeed)
    {
        return Math.Max(0d, Math.Min(1d, 1d - ((double)defenderSpeed - attackerSpeed) / 100d));
    }

    public static float BasicDamage(int attack, int defense)
    {
        return (float)(Math.Max(0, attack) * 100d / (100d + Math.Max(0, defense)));
    }

    public static double HitChance(this BattleCombatComponent self, BattleCombatComponent target) => HitChance(self.Speed, target.Speed);
    public static float BasicDamage(this BattleCombatComponent self, BattleCombatComponent target) => BasicDamage(self.Attack, target.Defense);
}
