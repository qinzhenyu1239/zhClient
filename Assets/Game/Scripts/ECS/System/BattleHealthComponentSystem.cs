using System;
using Framework.ECS;

[EntitySystemOf(typeof(BattleHealthComponent))]
public static class BattleHealthComponentSystem
{
    public static float Heal(this BattleHealthComponent self, float amount)
    {
        if (self == null || self.IsDisposed || self.CurrentHp <= 0) return 0;
        float applied = Math.Min(Math.Max(0, self.MaxHp - self.CurrentHp), Math.Max(0f, amount));
        self.CurrentHp += applied;
        return applied;
    }

    public static float ApplyDamage(this BattleHealthComponent self, float damage)
    {
        if (self == null || self.IsDisposed || self.CurrentHp <= 0) return 0;
        float applied = Math.Min(self.CurrentHp, Math.Max(0f, damage));
        self.CurrentHp -= applied;
        BattleUnit target = self.GetParent<BattleUnit>();
        if (self.CurrentHp <= 0) target?.Skills.CancelPendingSkill();
        BattlefieldComponent field = target?.GetParent<BattleEntity>()?.Battlefield;
        if (self.CurrentHp <= 0 && field != null && target.Position != null && field.GetUnitAt(target.SlotIndex) == target)
            field.Occupancy[target.SlotIndex] = null;
        return applied;
    }
}
