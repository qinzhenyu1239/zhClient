using System;
using System.Linq;
using Framework.ECS;

[EntitySystemOf(typeof(BattleBloodDomainComponent))]
public static class BattleBloodDomainComponentSystem
{
    public static bool IsBloodDomainActive(this BattleBloodDomainComponent self)
    {
        var unit = self?.GetParent<BattleUnit>();
        var round = unit?.GetParent<BattleEntity>()?.Round;
        return self != null && self.IsActive && unit != null && unit.CanAct() && round != null &&
            round.Phase != BattlePhase.Finished && round.RoundNumber >= self.FirstRound && round.RoundNumber <= self.LastRound;
    }

    public static void OnRoundStarted(this BattleBloodDomainComponent self, int round)
    {
        if (self != null && !self.IsDisposed && round > self.LastRound) self.Dispose();
    }

    public static void OnBloodDomainDeath(this BattleEntity battle, BattleUnit dead, int slot, int attack, int defense, int maxHp)
    {
        if (battle == null || !battle.IsActive || battle.Round == null || battle.Round.Phase == BattlePhase.Finished ||
            dead == null || dead.GetParent<BattleEntity>() != battle || slot < 0 || slot >= battle.Battlefield.SlotCount) return;
        // Exactly one active lord wins each death, even across opposing teams.
        var lord = battle.Battlefield.Units.Where(unit => unit != dead && unit.GetComponent<BattleBloodDomainComponent>().IsBloodDomainActive())
            .OrderBy(unit => BattleMovementComponentSystem.GridDistance(unit.SlotIndex, slot, battle.Battlefield.RowCount))
            .ThenBy(unit => unit.UnitId).FirstOrDefault();
        if (lord == null) return;
        var domain = lord.GetComponent<BattleBloodDomainComponent>();
        int from = lord.SlotIndex;
        lord.Position.TryPlaceBloodDomain(slot);
        var inheritance = lord.GetComponent<BattleBloodInheritanceComponent>() ?? lord.AddComponent<BattleBloodInheritanceComponent>();
        int oldAttack = lord.Attack, oldDefense = lord.Defense;
        inheritance.AttackBonus = (int)Math.Min(int.MaxValue, (long)inheritance.AttackBonus + (long)Math.Floor(attack * (double)domain.AttackRatio));
        int defenseGain = (int)Math.Min(int.MaxValue, Math.Floor(defense * (double)domain.DefenseRatio));
        int room = Math.Max(0, Math.Min(100, domain.DefenseCap) - lord.Defense);
        inheritance.DefenseBonus = (int)Math.Min(int.MaxValue, (long)inheritance.DefenseBonus + Math.Min(room, defenseGain));
        float healing = lord.Health.Heal(maxHp * domain.HealingRatio);
        battle.Round.PendingReactions.Enqueue(new BattleActionResult {
            Kind = BattleActionKind.BloodDomain, SkillId = BattleSkillDefinition.BloodDomain,
            RoundNumber = battle.Round.RoundNumber, Unit = lord, FromSlot = from, ToSlot = lord.SlotIndex,
            Healing = healing, Message = "血裔领域 · 攻+" + (lord.Attack - oldAttack) + " 防+" + (lord.Defense - oldDefense) +
                " 回复" + healing.ToString("0.##")
        });
    }
}
