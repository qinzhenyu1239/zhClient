using System;
using Framework.ECS;

[EntitySystemOf(typeof(BattlefieldComponent))]
public static class BattlefieldComponentSystem
{
    [EntitySystem]
    private static void Awake(this BattlefieldComponent self)
    {
        self.ColumnCount = BattleEntity.ColumnCount;
        self.RowCount = BattleEntity.RowCount;
        self.Occupancy = new BattleUnit[self.SlotCount];
    }

    [EntitySystem]
    private static void Destroy(this BattlefieldComponent self)
    {
        self.Members.Clear();
        Array.Clear(self.Occupancy, 0, self.Occupancy.Length);
    }

    public static BattleUnit GetUnitAt(this BattlefieldComponent self, int index)
    {
        if (self == null || self.IsDisposed || index < 0 || index >= self.SlotCount) return null;
        BattleUnit unit = self.Occupancy[index];
        return unit != null && !unit.IsDisposed && unit.Position != null && unit.SlotIndex == index ? unit : null;
    }

    public static bool ResolveVictory(this BattlefieldComponent self)
    {
        BattleEntity battle = self?.GetParent<BattleEntity>();
        BattleRoundComponent round = battle?.Round;
        if (self == null || self.IsDisposed || round == null) return false;
        if (battle.AliveCount(BattleSide.Ally) > 0 && battle.AliveCount(BattleSide.Enemy) > 0) return false;
        round.Winner = battle.AliveCount(BattleSide.Ally) > 0 ? BattleSide.Ally :
            battle.AliveCount(BattleSide.Enemy) > 0 ? BattleSide.Enemy : (BattleSide?)null;
        round.Phase = BattlePhase.Finished;
        foreach (var unit in self.Units)
        {
            unit.Skills.CancelPendingSkill();
            unit.GetComponent<BattleThundercloudComponent>()?.Dispose();
        }
        return true;
    }
}
