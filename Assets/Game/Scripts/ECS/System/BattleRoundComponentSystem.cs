using System;
using System.Collections.Generic;
using Framework.ECS;

public enum BattleActionKind { BasicAttack, Skill, Bleed, Lightning, Moonlight }

public sealed class BattleHitResult
{
    public int TargetSlot { get; internal set; }
    public bool Hit { get; internal set; }
    public float Damage { get; internal set; }
    public float Healing { get; internal set; }
    public float ShieldDamage { get; internal set; }
}

public sealed class BattleActionResult
{
    public float ShieldGained { get; internal set; }
    public BattleActionKind Kind { get; internal set; }
    public int SkillId { get; internal set; }
    public List<int> MoonMarkedSlots { get; } = new List<int>();
    public int MoonExplosions { get; internal set; }
    public string Message { get; internal set; }
    public float Healing { get; internal set; }
    public List<BattleHitResult> Hits { get; } = new List<BattleHitResult>();
    public int RoundNumber { get; internal set; }
    public BattleUnit Unit { get; internal set; }
    public BattleUnit Target { get; internal set; }
    public int FromSlot { get; internal set; }
    public int ToSlot { get; internal set; }
    public int TargetSlot { get; internal set; } = -1;
    public bool Hit { get; internal set; }
    public float Damage { get; internal set; }
}

[EntitySystemOf(typeof(BattleRoundComponent))]
public static class BattleRoundComponentSystem
{
    public const double PreparationSeconds = 15;
    public const double ActionIntervalSeconds = 1;

    [EntitySystem]
    private static void Awake(this BattleRoundComponent self)
    {
        self.RoundNumber = 1;
        self.Phase = BattlePhase.Preparation;
        self.PreparationRemaining = PreparationSeconds;
        self.ActionRemaining = ActionIntervalSeconds;
        self.Random = new Random();
        self.GetParent<BattleEntity>()?.GetComponent<BattleSkillPointComponent>()?.BeginRound(1);
    }

    [EntitySystem]
    private static void Destroy(this BattleRoundComponent self)
    {
        var battle = self.GetParent<BattleEntity>();
        if (battle?.Battlefield != null)
            foreach (var unit in battle.Battlefield.Units)
            {
                unit.Skills.CancelPendingSkill();
                unit.GetComponent<BattleThundercloudComponent>()?.Dispose();
                unit.GetComponent<BattleMoonShowerComponent>()?.Dispose();
            }
        self.ActionOrder.Clear();
        self.PendingActions.Clear();
        self.Random = null;
    }

    [EntitySystem]
    private static void Update(this BattleRoundComponent self)
    {
        BattleEntity battle = self.GetParent<BattleEntity>();
        if (battle?.Battlefield == null) return;
        double deltaSeconds = self.World.DeltaTime;
        while (deltaSeconds > 0 && self.Phase != BattlePhase.Finished)
        {
            double remaining = self.Phase == BattlePhase.Preparation ? self.PreparationRemaining : self.ActionRemaining;
            double elapsed = Math.Min(deltaSeconds, Math.Min(remaining, Math.Min(battle.NextCloudEvent(), battle.NextMoonEvent())));
            deltaSeconds -= elapsed;
            if (self.Phase == BattlePhase.Preparation)
                self.PreparationRemaining -= elapsed;
            else
                self.ActionRemaining -= elapsed;
            battle.AdvanceClouds(elapsed);
            if (self.Phase != BattlePhase.Finished) battle.AdvanceMoonShowers(elapsed);
            if (self.Phase == BattlePhase.Finished) break;
            if (self.Phase == BattlePhase.Preparation)
            {
                if (self.PreparationRemaining <= 0) self.BeginCombat();
            }
            else if (self.ActionRemaining <= 0)
            {
                self.ActionRemaining = ActionIntervalSeconds;
                self.ExecuteNextAction();
            }
        }
    }

    public static IReadOnlyList<BattleActionResult> DrainActions(this BattleRoundComponent self)
    {
        if (self == null || self.IsDisposed || self.PendingActions.Count == 0) return Array.Empty<BattleActionResult>();
        var results = new List<BattleActionResult>(self.PendingActions.Count);
        while (self.PendingActions.Count > 0) results.Add(self.PendingActions.Dequeue());
        return results;
    }

    private static void BeginCombat(this BattleRoundComponent self)
    {
        BattleEntity battle = self.GetParent<BattleEntity>();
        if (battle.Battlefield.ResolveVictory()) return;
        List<BattleUnit> order = self.ActionOrder;
        order.Clear();
        foreach (BattleUnit unit in battle.Battlefield.Units)
        {
            if (unit.CanAct()) order.Add(unit);
            else unit.Skills.CancelPendingSkill();
        }
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = self.Random.Next(i + 1);
            BattleUnit swap = order[i];
            order[i] = order[j];
            order[j] = swap;
        }
        self.NextUnit = 0;
        self.ActionRemaining = ActionIntervalSeconds;
        self.Phase = BattlePhase.Combat;
    }

    private static void ExecuteNextAction(this BattleRoundComponent self)
    {
        BattleEntity battle = self.GetParent<BattleEntity>();
        self.SkipUnavailable();
        if (self.NextUnit >= self.ActionOrder.Count) { self.BeginNextRound(); return; }
        BattleUnit unit = self.ActionOrder[self.NextUnit++];
        var result = new BattleActionResult { RoundNumber = self.RoundNumber, Unit = unit, FromSlot = unit.SlotIndex };
        if (unit.FindAdjacentEnemy() == null)
            unit.GetComponent<BattleMovementComponent>()?.TryMoveForward();
        result.ToSlot = unit.SlotIndex;
        battle.GetComponent<BattleEnemyAIComponent>()?.TrySelectSkill(unit);
        if (unit.Skills == null || !unit.Skills.TryExecutePending(result))
            unit.GetComponent<BattleBasicAttackComponent>()?.TryAttack(result);
        unit.Action.HasActedThisRound = true;
        self.PendingActions.Enqueue(result);
        if (!battle.Battlefield.ResolveVictory())
        {
            self.SkipUnavailable();
            if (self.NextUnit >= self.ActionOrder.Count) self.BeginNextRound();
        }
    }

    private static void SkipUnavailable(this BattleRoundComponent self)
    {
        while (self.NextUnit < self.ActionOrder.Count &&
            (!self.ActionOrder[self.NextUnit].CanAct() || self.ActionOrder[self.NextUnit].HasActedThisRound))
        {
            self.ActionOrder[self.NextUnit].Skills.CancelPendingSkill();
            self.NextUnit++;
        }
    }

    private static void BeginNextRound(this BattleRoundComponent self)
    {
        BattleEntity battle = self.GetParent<BattleEntity>();
        if (battle.Battlefield.ResolveVictory()) return;
        self.RoundNumber++;
        self.Phase = BattlePhase.Preparation;
        self.PreparationRemaining = PreparationSeconds;
        battle.GetComponent<BattleSkillPointComponent>()?.BeginRound(self.RoundNumber);
        foreach (BattleUnit unit in battle.Battlefield.Units)
        {
            unit.Skills.CancelPendingSkill();
            if (unit.Action != null) unit.Action.HasActedThisRound = false;
            unit.GetComponent<BattleShieldComponent>()?.OnRoundStarted(self.RoundNumber);
            unit.GetComponent<BattleSilenceComponent>()?.OnRoundStarted(self.RoundNumber);
            unit.GetComponent<BattleBleedComponent>()?.OnRoundStarted(self);
        }
        if (!battle.Battlefield.ResolveVictory()) battle.GetComponent<BattleEnemyAIComponent>()?.PlanSkills();
    }
}
