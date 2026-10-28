using Framework.ECS;

[EntitySystemOf(typeof(BattlePositionComponent))]
public static class BattlePositionComponentSystem
{
    [EntitySystem]
    private static void Awake(this BattlePositionComponent self) { self.SlotIndex = -1; }

    [EntitySystem]
    private static void Destroy(this BattlePositionComponent self)
    {
        BattleUnit unit = self.GetParent<BattleUnit>();
        BattlefieldComponent field = unit?.GetParent<BattleEntity>()?.Battlefield;
        if (field != null && self.SlotIndex >= 0 && self.SlotIndex < field.SlotCount && field.Occupancy[self.SlotIndex] == unit)
            field.Occupancy[self.SlotIndex] = null;
        self.SlotIndex = -1;
    }

    public static bool TryPlace(this BattlePositionComponent self, int destination)
    {
        BattleUnit unit = self?.GetParent<BattleUnit>();
        BattlefieldComponent field = unit?.GetParent<BattleEntity>()?.Battlefield;
        if (self == null || self.IsDisposed || unit == null || unit.IsDisposed || field == null ||
            destination < 0 || destination >= field.SlotCount || !field.Members.Contains(unit)) return false;
        BattleUnit occupied = field.GetUnitAt(destination);
        if (occupied != null && occupied != unit) return false;
        if (self.SlotIndex >= 0 && self.SlotIndex < field.SlotCount && field.Occupancy[self.SlotIndex] == unit)
            field.Occupancy[self.SlotIndex] = null;
        field.Occupancy[destination] = unit;
        self.SlotIndex = destination;
        return true;
    }
}
