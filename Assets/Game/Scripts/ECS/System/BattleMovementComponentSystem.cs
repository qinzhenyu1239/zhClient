using Framework.ECS;

[EntitySystemOf(typeof(BattleMovementComponent))]
public static class BattleMovementComponentSystem
{
    public static bool TryMoveForward(this BattleMovementComponent self)
    {
        BattleUnit unit = self?.GetParent<BattleUnit>();
        BattlefieldComponent field = unit?.GetParent<BattleEntity>()?.Battlefield;
        if (self == null || self.IsDisposed || !self.IsActive || unit == null || !unit.IsAlive ||
            unit.Identity == null || unit.Position == null || field == null || field.GetUnitAt(unit.SlotIndex) != unit) return false;
        int destination = unit.SlotIndex + (unit.Side == BattleSide.Ally ? field.RowCount : -field.RowCount);
        return unit.Position.TryPlace(destination);
    }
}
