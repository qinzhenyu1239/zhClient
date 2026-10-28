using Framework.ECS;

[EntitySystemOf(typeof(BattleEntity))]
public static class BattleEntitySystem
{
    public static BattleUnit GetUnitAt(this BattleEntity self, int slotIndex) => self?.Battlefield?.GetUnitAt(slotIndex);

    public static int AliveCount(this BattleEntity self, BattleSide side)
    {
        if (self == null || self.IsDisposed || self.Battlefield == null) return 0;
        int count = 0;
        foreach (BattleUnit unit in self.Battlefield.Units)
            if (unit.IsAlive && unit.Identity != null && unit.Side == side) count++;
        return count;
    }
}
