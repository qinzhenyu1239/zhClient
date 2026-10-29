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
        if (unit.FindAdjacentEnemy() != null) return false;
        var assassin = unit.GetComponent<BattleAssassinComponent>();
        if (assassin != null && !assassin.HasInfiltrated) return assassin.TryApproachRear();
        int destination = unit.SlotIndex + unit.ForwardStep();
        return unit.Position.TryPlace(destination);
    }
}
