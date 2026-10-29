using Framework.ECS;

[EntitySystemOf(typeof(BattleUnit))]
public static class BattleUnitSystem
{
    // Slots are column-major; vertical neighbors must stay inside the same column.
    public static BattleUnit FindAdjacentEnemy(this BattleUnit self)
    {
        BattlefieldComponent field = self?.GetParent<BattleEntity>()?.Battlefield;
        if (self == null || !self.IsActive || !self.IsAlive || self.Identity == null ||
            self.Position == null || field == null || field.GetUnitAt(self.SlotIndex) != self) return null;
        int forward = self.ForwardStep();
        int row = self.SlotIndex % field.RowCount;
        return EnemyAt(self.SlotIndex + forward) ??
            (row > 0 ? EnemyAt(self.SlotIndex - 1) : null) ??
            (row + 1 < field.RowCount ? EnemyAt(self.SlotIndex + 1) : null) ??
            EnemyAt(self.SlotIndex - forward);

        BattleUnit EnemyAt(int slot)
        {
            BattleUnit target = field.GetUnitAt(slot);
            return target != null && target.IsActive && target.IsAlive && target.Identity != null &&
                target.Combat != null && target.Side != self.Side ? target : null;
        }
    }

    public static bool CanAct(this BattleUnit self)
    {
        BattleEntity battle = self?.GetParent<BattleEntity>();
        return self != null && self.IsActive && self.IsAlive && self.Identity != null && self.Position != null &&
            self.Action != null && battle != null && battle.GetUnitAt(self.SlotIndex) == self;
    }

    [EntitySystem]
    private static void Destroy(this BattleUnit self)
    {
        self.GetParent<BattleEntity>()?.Battlefield?.Members.Remove(self);
    }
}
