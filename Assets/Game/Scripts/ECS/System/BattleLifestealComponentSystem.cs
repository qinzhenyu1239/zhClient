using Framework.ECS;

[EntitySystemOf(typeof(BattleLifestealComponent))]
public static class BattleLifestealComponentSystem
{
    public static float HealFromDirectDamage(this BattleLifestealComponent self, float damage)
    {
        if (self == null || self.IsDisposed || !self.IsActive || damage <= 0) return 0;
        BattleUnit unit = self.GetParent<BattleUnit>();
        return unit?.Health.Heal(damage * self.Stacks * self.RatioPerStack) ?? 0;
    }
}
