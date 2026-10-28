using System;
using System.Linq;
using System.Collections.Generic;

static class SkillChecks
{
    sealed class FixedRandom : Random
    {
        public override int Next(int maxValue) => maxValue - 1;
        public override int Next(int minValue, int maxValue) => maxValue - 1;
        public override double NextDouble() => 0.99;
    }
    static void Check(bool condition, string message) { if (!condition) throw new Exception("Skill: " + message); }
    static void Near(float actual, float expected, string message) => Check(Math.Abs(actual - expected) < 0.01, message + " actual=" + actual);
    static BattleEntity Create()
    {
        var config = ExcelDataMgr.Instance.GetConfigData<character>("1001");
        var records = ExcelDataMgr.Instance.GetConfigDatas<skill>(null);
        return BattleInitializationSystem.CreateBattle(config, random: new FixedRandom(), skillConfigs: records);
    }
    static BattleActionResult Cast(BattleUnit unit, int id)
    {
        Check(unit.Skills.TryQueue(id, out string reason), "queue " + id + ": " + reason);
        var result = new BattleActionResult { Unit = unit, FromSlot = unit.SlotIndex, ToSlot = unit.SlotIndex };
        Check(unit.Skills.TryExecutePending(result), "execute " + id);
        return result;
    }
    static IReadOnlyList<BattleActionResult> Tick(BattleEntity battle, double time)
    {
        battle.World.Update(time);
        return battle.Round.DrainActions();
    }
    public static void Run()
    {
        using var battle = Create();
        var unit = battle.GetUnitAt(15);
        var target = battle.GetUnitAt(18);
        var pool = battle.GetComponent<BattleSkillPointComponent>();
        Check(pool.Current == 30 && pool.PointsPerRound == 30, "30 points per round");
        Check(unit.Skills.TryQueue(100101, out _), "queue in preparation");
        Check(pool.Current == 29, "immediate cost from CSV");
        unit.Skills.TryQueue(100101, out _);
        Check(pool.Current == 29, "same choice charges once");
        unit.Skills.TryQueue(100102, out _);
        Check(pool.Current == 29 && unit.Skills.PendingSkillId == 100102, "atomic replacement refunds old selection");
        Check(unit.Skills.CancelPendingSkill() == 1 && pool.Current == 30, "cancel refund");
        pool.TrySpend(30);
        Check(!unit.Skills.TryQueue(100101, out _), "insufficient points");
        pool.BeginRound(1);
        unit.Skills.TryQueue(100101, out _);
        pool.BeginRound(2);
        pool.TrySpend(5);
        unit.Skills.CancelPendingSkill();
        Check(pool.Current == 25, "old round reservation cannot credit new round");
        pool.BeginRound(1);
        battle.Round.Phase = BattlePhase.Combat;
        var strike = Cast(unit, 100101);
        Check(strike.Hits.Count == 2 && strike.Target == target, "two hits on nearest target");
        Near(strike.Damage, 100, "normal armor damage for both hits");
        var bleed = target.GetComponent<BattleBleedComponent>();
        Check(bleed.RemainingTicks == 2 && bleed.NextTickRound == 2, "bleed delayed two ticks");
        float hp = target.CurrentHp;
        bleed.OnRoundStarted(battle.Round);
        Near(target.CurrentHp, hp, "no bleed on cast round");
        battle.Round.RoundNumber = 2;
        bleed.OnRoundStarted(battle.Round);
        Near(target.CurrentHp, hp - 120, "10 percent max HP ignores armor");
        battle.Round.RoundNumber = 3;
        bleed.OnRoundStarted(battle.Round);
        Check(target.GetComponent<BattleBleedComponent>() == null, "bleed expires after two ticks");
        Near(target.CurrentHp, hp - 240, "two ticks total");
        battle.Round.RoundNumber = 1;
        pool.BeginRound(1);
        for (int i = 0; i < 5; i++) Cast(unit, 100102);
        Check(!unit.Skills.TryQueue(100102, out _) && pool.Current == 25, "five stacks cap does not consume points");
        var lifesteal = unit.GetComponent<BattleLifestealComponent>();
        Check(lifesteal.Stacks == 5, "lifesteal stacks");
        unit.Health.ApplyDamage(200);
        hp = unit.CurrentHp;
        var attack = new BattleActionResult();
        unit.GetComponent<BattleBasicAttackComponent>().TryAttack(attack);
        Near(unit.CurrentHp, hp + 25, "50 percent lifesteal on actual direct damage");
        Near(attack.Healing, 25, "healing result");
        Cast(unit, 100103);
        var combo = unit.GetComponent<BattleComboAttackComponent>();
        Check(combo.FirstRound == 2 && combo.LastRound == 4 && combo.HitCount(battle.Round) == 1, "cast action grants following three rounds");
        for (int round = 2; round <= 4; round++)
        {
            battle.Round.RoundNumber = round;
            Check(combo.HitCount(battle.Round) == 5, "configured 1-5 random hit range");
        }
        int comboPoints = pool.Current;
        var comboAttack = new BattleActionResult();
        unit.GetComponent<BattleBasicAttackComponent>().TryAttack(comboAttack);
        Check(comboAttack.Hits.Count == 5 && comboAttack.Target == target, "combo executes five hits on the same target");
        Check(pool.Current == comboPoints, "enhanced basic hits do not spend more points");
        battle.Round.RoundNumber = 5;
        combo.OnRoundStarted(5);
        Check(unit.GetComponent<BattleComboAttackComponent>() == null, "combo expires after third round");
        battle.Round.RoundNumber = 1;
        pool.BeginRound(1);
        target.Health.CurrentHp = 30;
        unit.Health.CurrentHp = 1000;
        strike = Cast(unit, 100101);
        Check(strike.Hits.Count == 1 && !target.IsAlive, "stop on death without retargeting");
        Near(strike.Healing, 15, "overkill heals actual lost HP only");
        Check(target.GetComponent<BattleBleedComponent>() == null, "dead target gets no bleed");
        var rear = battle.GetUnitAt(9);
        rear.Skills.TryQueue(100101, out _);
        int before = pool.Current;
        var failed = new BattleActionResult();
        Check(!rear.Skills.TryExecutePending(failed) && pool.Current == before + 1, "out of range refunds");
        rear.Skills.TryQueue(100102, out _);
        before = pool.Current;
        rear.Health.ApplyDamage(rear.MaxHp);
        Check(pool.Current == before + 1, "death refunds pending command");
        var other = battle.GetUnitAt(10);
        other.Skills.TryQueue(100102, out _);
        before = pool.Current;
        other.RemoveComponent<BattleSkillComponent>();
        Check(pool.Current == before + 1, "skill component removal refunds");
        Check(unit.GetComponent<BattleLifestealComponent>() != other.GetComponent<BattleLifestealComponent>(), "status component isolation");
        Check(ExcelDataMgr.Instance.GetConfigData<character>("1001").hp == 1200, "runtime does not mutate source config");
        Check(ExcelDataMgr.Instance.GetConfigDatas<skill>(null).All(s => s.skillPointCost == 1), "CSV costs imported");

        using var scheduled = Create();
        var actor = scheduled.GetUnitAt(15);
        var victim = scheduled.GetUnitAt(18);
        // Freeze the other units, so one second executes exactly this actor and starts round two.
        foreach (var u in scheduled.Battlefield.Units) if (u != actor) u.RemoveComponent<BattleActionComponent>();
        actor.Skills.TryQueue(100101, out _);
        var results = Tick(scheduled, 16);
        Check(results.Count == 2 && results[0].Kind == BattleActionKind.Skill && results[1].Kind == BattleActionKind.Bleed, "action queued before next preparation bleed");
        Check(scheduled.Round.RoundNumber == 2 && scheduled.Round.Phase == BattlePhase.Preparation, "turn lifecycle integrates skill and DOT");
        Check(scheduled.GetComponent<BattleSkillPointComponent>().Current == 30, "next preparation resets points");
        Near(victim.CurrentHp, 980, "scheduled two hits plus first bleed");
        // Refresh, rather than stack, and ensure DOT never triggers lifesteal.
        scheduled.Round.Phase = BattlePhase.Combat;
        Cast(actor, 100101);
        var refreshed = victim.GetComponent<BattleBleedComponent>();
        Check(refreshed.RemainingTicks == 2 && refreshed.NextTickRound == 3, "reapplication refreshes bleed");
        var steal = victim.AddComponent<BattleLifestealComponent>();
        steal.Stacks = 5; steal.RatioPerStack = 0.1f;
        scheduled.Round.RoundNumber = 3;
        float victimHp = victim.CurrentHp;
        refreshed.OnRoundStarted(scheduled.Round);
        Near(victim.CurrentHp, victimHp - 120, "bleed does not lifesteal");
        actor.Combat.Speed = 0;
        victim.Combat.Speed = 200;
        actor.Skills.CancelPendingSkill();
        scheduled.GetComponent<BattleSkillPointComponent>().BeginRound(3);
        var missed = Cast(actor, 100101);
        Check(!missed.Hit && missed.Hits.Count == 2, "each strike independently checks hit");
        Check(victim.GetComponent<BattleBleedComponent>().RemainingTicks == 1, "all misses do not refresh bleed");
        using var separate = Create();
        Check(separate.GetComponent<BattleSkillPointComponent>().Current == 30, "separate battle pool isolation");
        var separateActor = separate.GetUnitAt(15);
        separateActor.Action.HasActedThisRound = true;
        Check(!separateActor.Skills.TryQueue(100101, out _), "acted unit cannot buy an extra action");
        separateActor.Action.HasActedThisRound = false;
        separateActor.Skills.TryQueue(100101, out _);
        int reserved = separate.GetComponent<BattleSkillPointComponent>().Current;
        separateActor.Position.TryPlace(12);
        Check(separateActor.SlotIndex == 15, "occupied destination is rejected");
        separateActor.RemoveComponent<BattleCombatComponent>();
        separate.Round.Phase = BattlePhase.Combat;
        Check(!separateActor.Skills.TryExecutePending(new BattleActionResult()) && separate.GetComponent<BattleSkillPointComponent>().Current == reserved + 1, "missing combat data refunds safely");
        Console.WriteLine("PASS: skill CSV costs, immediate reservation/refunds, death/removal/range refunds, round budget isolation, two-hit bleed, refresh/miss, five-stack lifesteal, actual damage/heal, combo duration, scheduled action/DOT order, independent battles.");
    }
}
