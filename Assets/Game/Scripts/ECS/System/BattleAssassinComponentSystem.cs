using Framework.ECS;

[EntitySystemOf(typeof(BattleAssassinComponent))]
public static class BattleAssassinComponentSystem
{
    public static int ForwardStep(this BattleUnit unit)
    {
        int step = unit.GetParent<BattleEntity>().Battlefield.RowCount * (unit.Side == BattleSide.Ally ? 1 : -1);
        return unit.GetComponent<BattleAssassinComponent>()?.HasInfiltrated == true ? -step : step;
    }

    public static bool TryInfiltrate(this BattleAssassinComponent self)
    {
        BattleUnit unit = self?.GetParent<BattleUnit>();
        BattlefieldComponent field = unit?.GetParent<BattleEntity>()?.Battlefield;
        if (self == null || !self.IsActive || !self.OpeningInfiltrationEligible || self.HasInfiltrated || !unit.CanAct() || field == null) return false;
        int enemyColumn = unit.Side == BattleSide.Ally ? field.ColumnCount - 1 : 0;
        int row = unit.SlotIndex % field.RowCount;
        for (int offset = 0; offset < field.RowCount; offset++)
        {
            int destination = enemyColumn * field.RowCount + (row + offset) % field.RowCount;
            if (!unit.Position.TryPlace(destination)) continue;
            self.HasInfiltrated = true;
            return true;
        }
        return false;
    }

}
