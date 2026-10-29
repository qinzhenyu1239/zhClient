using System;
using Framework.ECS;

[EntitySystemOf(typeof(BattleShieldComponent))]
public static class BattleShieldComponentSystem
{
    public static void AddPermanentShield(this BattleUnit unit, float amount, int defenseBonus)
    {
        if (unit == null || !unit.IsAlive || amount <= 0 || defenseBonus < 0) return;
        var shield = unit.GetComponent<BattleShieldComponent>() ?? unit.AddComponent<BattleShieldComponent>();
        shield.Layers.Add(new BattleShieldLayer { Amount = amount, ExpiresAtRound = int.MaxValue, DefenseBonus = defenseBonus });
        shield.Layers.Sort((a, b) => a.ExpiresAtRound.CompareTo(b.ExpiresAtRound));
    }

    public static void AddShield(this BattleUnit unit, float amount, int rounds)
    {
        var round = unit?.GetParent<BattleEntity>()?.Round;
        if (unit == null || !unit.IsAlive || round == null || amount <= 0 || rounds <= 0) return;
        var shield = unit.GetComponent<BattleShieldComponent>() ?? unit.AddComponent<BattleShieldComponent>();
        shield.Layers.Add(new BattleShieldLayer { Amount = amount, ExpiresAtRound = round.RoundNumber + rounds });
        shield.Layers.Sort((a, b) => a.ExpiresAtRound.CompareTo(b.ExpiresAtRound));
    }

    public static float Absorb(this BattleShieldComponent self, float damage)
    {
        if (self == null || !self.IsActive || damage <= 0) return 0;
        float remaining = damage;
        foreach (var layer in self.Layers)
        {
            float absorbed = Math.Min(layer.Amount, remaining);
            layer.Amount -= absorbed;
            remaining -= absorbed;
            if (remaining <= 0) break;
        }
        self.Layers.RemoveAll(layer => layer.Amount <= 0);
        if (self.Layers.Count == 0) self.Dispose();
        return damage - remaining;
    }

    public static void OnRoundStarted(this BattleShieldComponent self, int round)
    {
        if (self == null || self.IsDisposed) return;
        self.Layers.RemoveAll(layer => layer.ExpiresAtRound <= round);
        if (self.Layers.Count == 0) self.Dispose();
    }
}
