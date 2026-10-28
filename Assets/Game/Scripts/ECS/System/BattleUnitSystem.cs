using Framework.ECS;

[EntitySystemOf(typeof(BattleUnit))]
public static class BattleUnitSystem
{
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
