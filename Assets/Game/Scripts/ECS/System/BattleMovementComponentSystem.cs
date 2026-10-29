using System;
using System.Collections.Generic;
using System.Linq;
using Framework.ECS;

[EntitySystemOf(typeof(BattleMovementComponent))]
public static class BattleMovementComponentSystem
{
    // Compatibility for older callers; movement no longer has a fixed forward direction.
    public static bool TryMoveForward(this BattleMovementComponent self) => self.TryMoveTowardEnemy();

    public static float MovementStopRange(this BattleUnit unit)
    {
        float basicRange = Math.Max(0, unit.AttackRange);
        var skills = unit.Skills;
        int id = skills?.PendingSkillId ?? 0;
        if (id == 0 || !skills.CanQueue(id, out _)) return basicRange;
        switch (id)
        {
            case BattleSkillDefinition.DoubleStrike:
            case BattleSkillDefinition.ThunderStrike:
            case BattleSkillDefinition.ChainLightning:
            case BattleSkillDefinition.Thundercloud:
            case BattleSkillDefinition.JudgmentBeam:
            case BattleSkillDefinition.MoonCleave:
            case BattleSkillDefinition.MoonShower:
                return Math.Max(basicRange, skills.GetDefinition(id).Range);
            default:
                // Self buffs and teleport-destination attack ranges do not define a remote firing range.
                return basicRange;
        }
    }

    public static bool TryMoveTowardEnemy(this BattleMovementComponent self)
    {
        BattleUnit unit = self?.GetParent<BattleUnit>();
        BattlefieldComponent field = unit?.GetParent<BattleEntity>()?.Battlefield;
        if (self == null || self.IsDisposed || !self.IsActive || unit == null || !unit.IsAlive ||
            unit.Identity == null || unit.Position == null || field == null || field.GetUnitAt(unit.SlotIndex) != unit) return false;
        var targets = field.Units.Where(target => target != unit && target.IsActive && target.IsAlive &&
            target.Identity != null && target.Side != unit.Side && target.Position != null && target.Combat != null &&
            field.GetUnitAt(target.SlotIndex) == target)
            .OrderBy(target => GridDistance(unit.SlotIndex, target.SlotIndex, field.RowCount)).ThenBy(target => target.SlotIndex).ToList();
        if (targets.Count == 0) return false;
        var assassin = unit.GetComponent<BattleAssassinComponent>();
        if (assassin != null && assassin.TryInfiltrate()) return true;
        float range = Math.Max(1, unit.MovementStopRange());
        if (GridDistance(unit.SlotIndex, targets[0].SlotIndex, field.RowCount) <= range) return false;
        foreach (var target in targets)
        {
            int step = FindStep(unit, target, Math.Max(1, range));
            if (step >= 0) return unit.Position.TryPlace(step);
        }
        return false;
    }

    public static int GridDistance(int a, int b, int rows) => Math.Abs(a / rows - b / rows) + Math.Abs(a % rows - b % rows);

    private static IEnumerable<int> Neighbors(int slot, BattlefieldComponent field)
    {
        if (slot / field.RowCount > 0) yield return slot - field.RowCount;
        if (slot / field.RowCount + 1 < field.ColumnCount) yield return slot + field.RowCount;
        if (slot % field.RowCount > 0) yield return slot - 1;
        if (slot % field.RowCount + 1 < field.RowCount) yield return slot + 1;
    }

    private static int FindStep(BattleUnit unit, BattleUnit target, float range)
    {
        var field = unit.GetParent<BattleEntity>().Battlefield;
        int start = unit.SlotIndex;
        var firstSteps = new int[field.SlotCount];
        for (int i = 0; i < firstSteps.Length; i++) firstSteps[i] = -1;
        var queue = new Queue<int>();
        firstSteps[start] = start;
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            if (current != start && GridDistance(current, target.SlotIndex, field.RowCount) <= range) return firstSteps[current];
            foreach (int next in Neighbors(current, field)
                .OrderBy(slot => GridDistance(slot, target.SlotIndex, field.RowCount)).ThenBy(slot => slot))
            {
                if (firstSteps[next] >= 0 || !unit.CanEnterSlot(next) || field.GetUnitAt(next) != null) continue;
                firstSteps[next] = current == start ? next : firstSteps[current];
                queue.Enqueue(next);
            }
        }
        return -1;
    }
}
