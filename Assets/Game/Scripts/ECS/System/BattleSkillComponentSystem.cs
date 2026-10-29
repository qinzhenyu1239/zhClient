using System;
using Framework.ECS;

[EntitySystemOf(typeof(BattleSkillComponent))]
public static class BattleSkillComponentSystem
{
    [EntitySystem]
    private static void Destroy(this BattleSkillComponent self)
    {
        self.CancelPendingSkill();
        self.Definitions.Clear();
    }

    public static BattleSkillDefinition GetDefinition(this BattleSkillComponent self, int id)
    {
        return self != null && !self.IsDisposed && self.Definitions.TryGetValue(id, out BattleSkillDefinition definition) ? definition : null;
    }

    public static bool CanQueue(this BattleSkillComponent self, int id, out string reason)
    {
        BattleUnit unit = self?.GetParent<BattleUnit>();
        BattleEntity battle = unit?.GetParent<BattleEntity>();
        BattleSkillDefinition definition = self.GetDefinition(id);
        if (self == null || self.IsDisposed || !self.IsActive || unit == null || !unit.CanAct() || unit.HasActedThisRound ||
            battle?.Round == null || battle.Round.Phase == BattlePhase.Finished)
        { reason = "该单位当前不能选择技能，已行动的单位请在下回合选择"; return false; }
        if (unit.IsSilenced()) { reason = "沉默中，本回合无法普攻或释放技能"; return false; }
        if (definition == null || (id != self.Skill1Id && id != self.Skill2Id && id != self.Skill3Id) ||
            (id != BattleSkillDefinition.DoubleStrike && id != BattleSkillDefinition.Lifesteal && id != BattleSkillDefinition.ComboAttack &&
             id != BattleSkillDefinition.ThunderStrike && id != BattleSkillDefinition.ChainLightning && id != BattleSkillDefinition.Thundercloud))
        { reason = "技能配置不存在或暂不支持"; return false; }
        if (unit.Combat == null) { reason = "未挂载战斗属性组件"; return false; }
        if (id == BattleSkillDefinition.Lifesteal && unit.GetComponent<BattleLifestealComponent>()?.Stacks >= definition.Int(2))
        { reason = "吸血已达到最大层数"; return false; }
        BattleSkillPointComponent pool = battle.GetComponent<BattleSkillPointComponent>();
        int refundable = self.ReservedPool == pool && self.ReservedRound == pool?.RoundNumber && self.ReservedSide == unit.Side ? self.ReservedCost : 0;
        if (pool == null || definition.PointCost > pool.CurrentFor(unit.Side) + refundable)
        { reason = "共享技能点不足"; return false; }
        reason = string.Empty;
        return true;
    }

    public static bool TryQueue(this BattleSkillComponent self, int id, out string reason)
    {
        if (!self.CanQueue(id, out reason)) return false;
        if (self.PendingSkillId == id && self.ReservedSide == self.GetParent<BattleUnit>().Side) return true;
        BattleUnit unit = self.GetParent<BattleUnit>();
        BattleEntity battle = unit.GetParent<BattleEntity>();
        BattleSkillDefinition definition = self.GetDefinition(id);
        self.CancelPendingSkill();
        BattleSkillPointComponent pool = battle.GetComponent<BattleSkillPointComponent>();
        if (!pool.TrySpend(definition.PointCost, unit.Side)) { reason = "共享技能点不足"; return false; }
        self.PendingSkillId = id;
        self.ReservedCost = definition.PointCost;
        self.ReservedRound = battle.Round.RoundNumber;
        self.ReservedPool = pool;
        self.ReservedSide = unit.Side;
        return true;
    }

    public static int CancelPendingSkill(this BattleSkillComponent self)
    {
        if (self == null) return 0;
        BattleSkillPointComponent pool = self.ReservedPool;
        int before = pool.CurrentFor(self.ReservedSide);
        pool.Refund(self.ReservedCost, self.ReservedRound, self.ReservedSide);
        int refunded = pool.CurrentFor(self.ReservedSide) - before;
        ClearReservation(self);
        return refunded;
    }

    public static bool TryExecutePending(this BattleSkillComponent self, BattleActionResult action)
    {
        if (self == null || self.IsDisposed || self.PendingSkillId == 0) return false;
        BattleUnit unit = self.GetParent<BattleUnit>();
        BattleEntity battle = unit?.GetParent<BattleEntity>();
        BattleSkillDefinition definition = self.GetDefinition(self.PendingSkillId);
        if (!self.CanQueue(self.PendingSkillId, out string reason) || battle.Round.Phase != BattlePhase.Combat ||
            self.ReservedRound != battle.Round.RoundNumber || self.ReservedSide != unit.Side || self.ReservedPool == null || self.ReservedPool != battle.GetComponent<BattleSkillPointComponent>() || self.ReservedPool.IsDisposed)
        {
            int refund = self.CancelPendingSkill();
            action.Message = "技能未释放，退还 " + refund + " 点；" + reason;
            return false;
        }
        BattleUnit target = null;
        if (definition.Id == BattleSkillDefinition.DoubleStrike || definition.Id == BattleSkillDefinition.ThunderStrike ||
            definition.Id == BattleSkillDefinition.ChainLightning || definition.Id == BattleSkillDefinition.Thundercloud)
        {
            target = unit.Combat.FindTargetInRange(definition.Range);
            if (target == null)
            {
                int refund = self.CancelPendingSkill();
                action.Message = "技能目标不在范围，退还 " + refund + " 点";
                return false;
            }
        }
        ClearReservation(self);
        action.Kind = BattleActionKind.Skill;
        action.SkillId = definition.Id;
        switch (definition.Id)
        {
            case BattleSkillDefinition.DoubleStrike:
                for (int i = 0; i < definition.Int(1) && target.IsAlive; i++) unit.Combat.ResolveDirectHit(target, action);
                if (action.Hit && target.IsAlive)
                {
                    BattleBleedComponent bleed = target.GetComponent<BattleBleedComponent>() ?? target.AddComponent<BattleBleedComponent>();
                    bleed.NextTickRound = battle.Round.RoundNumber + 1;
                    bleed.RemainingTicks = definition.Int(2);
                    bleed.MaxHpRatio = definition.Percent(3);
                }
                action.Message = "技能1 · 连击流血";
                break;
            case BattleSkillDefinition.Lifesteal:
                BattleLifestealComponent lifesteal = unit.GetComponent<BattleLifestealComponent>() ?? unit.AddComponent<BattleLifestealComponent>();
                lifesteal.MaxStacks = definition.Int(2);
                lifesteal.RatioPerStack = definition.Percent(1);
                lifesteal.Stacks = Math.Min(lifesteal.MaxStacks, lifesteal.Stacks + 1);
                action.Message = "吸血 " + (lifesteal.Stacks * lifesteal.RatioPerStack * 100).ToString("0.##") + "%（" + lifesteal.Stacks + "/" + lifesteal.MaxStacks + "）";
                break;
            case BattleSkillDefinition.ComboAttack:
                BattleComboAttackComponent combo = unit.GetComponent<BattleComboAttackComponent>() ?? unit.AddComponent<BattleComboAttackComponent>();
                combo.FirstRound = battle.Round.RoundNumber + 1;
                combo.LastRound = battle.Round.RoundNumber + definition.Int(1);
                combo.MinHits = definition.Int(2);
                combo.MaxHits = definition.Int(3);
                action.Message = "后 " + definition.Int(1) + " 回合普攻连击";
                break;
            case BattleSkillDefinition.ThunderStrike:
            case BattleSkillDefinition.ChainLightning:
            case BattleSkillDefinition.Thundercloud:
                self.CastThunderSkill(definition, target, action);
                break;
        }
        return true;
    }

    private static void ClearReservation(BattleSkillComponent self)
    {
        self.PendingSkillId = 0;
        self.ReservedCost = 0;
        self.ReservedRound = 0;
        self.ReservedPool = null;
    }
}
