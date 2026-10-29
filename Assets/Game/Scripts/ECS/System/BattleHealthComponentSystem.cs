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
        return self.ApplyDamage(damage, out _);
    }

    public static float ApplyDamage(this BattleHealthComponent self, float damage, out float absorbed)
    {
        absorbed = 0;
        if (self == null || self.IsDisposed || self.CurrentHp <= 0) return 0;
        BattleUnit target = self.GetParent<BattleUnit>();
        damage = Math.Max(0f, damage);
        absorbed = target?.GetComponent<BattleShieldComponent>().Absorb(damage) ?? 0;
        float applied = Math.Min(self.CurrentHp, damage - absorbed);
        self.CurrentHp -= applied;
        self.FinalizeDeath();
        if (applied > 0) target?.GetParent<BattleEntity>()?.CheckJudgments();
        return applied;
    }

    // Executions remove remaining HP directly, bypassing armor and every shield layer.
    public static float Execute(this BattleHealthComponent self)
    {
        if (self == null || self.IsDisposed || self.CurrentHp <= 0) return 0;
        float applied = self.CurrentHp;
        self.CurrentHp = 0;
        self.FinalizeDeath();
        return applied;
    }

    private static void FinalizeDeath(this BattleHealthComponent self)
    {
        BattleUnit target = self.GetParent<BattleUnit>();
        if (self.CurrentHp <= 0)
        {
            target?.Skills.CancelPendingSkill();
            target?.GetComponent<BattleMoonMarkComponent>()?.Dispose();
            target?.GetComponent<BattleMoonShowerComponent>()?.Dispose();
            target?.GetComponent<BattleShieldComponent>()?.Dispose();
            target?.GetComponent<BattleJudgmentComponent>()?.Dispose();
        }
        BattlefieldComponent field = target?.GetParent<BattleEntity>()?.Battlefield;
        if (self.CurrentHp <= 0 && field != null && target.Position != null && field.GetUnitAt(target.SlotIndex) == target)
            field.Occupancy[target.SlotIndex] = null;
    }
}
