using System;
using System.Linq;

static class CooldownChecks
{
    sealed class KeepOrderRandom : Random
    {
        public override int Next(int maxValue) => maxValue - 1;
        public override double NextDouble() => 0;
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Cooldown: " + message);
    }

    static BattleEntity Create(int cooldown = 2)
    {
        var config = new character { ID = 1001, name = "fixture", hp = 10000, attack = 100,
            defense = 0, speed = 100, attackRange = 1, battleType = 1,
            skill1Id = 100101, skill2Id = 100102 };
        var definitions = new[]
        {
            new skill { ID = 100101, param1 = "2", param2 = "2", param3 = "1", skillRange = 1, skillPointCost = 1, skillCd = cooldown },
            new skill { ID = 100102, param1 = "10", param2 = "5", skillRange = 99, skillPointCost = 1, skillCd = cooldown }
        };
        return BattleInitializationSystem.CreateBattle(config, random: new KeepOrderRandom(), skillConfigs: definitions);
    }

    static void Cast(BattleUnit unit, int id)
    {
        Check(unit.Skills.TryQueue(id, out string reason), "queue: " + reason);
        Check(unit.Skills.TryExecutePending(new BattleActionResult()), "successful cast");
    }

    public static void Run()
    {
        var records = ExcelDataMgr.Instance.GetConfigDatas<skill>(null);
        Check(records.Count == 9 && records.All(record => record.skillCd >= 0), "real CSV reads round-based skillCd for all skills");
        Check(records.All(record => new BattleSkillDefinition(record).CooldownRounds == record.skillCd), "immutable definition snapshots CD");
        using var battle = Create();
        using var other = Create();
        BattleUnit unit = battle.GetUnitAt(9);
        BattleUnit friend = battle.GetUnitAt(10);
        var pool = battle.GetComponent<BattleSkillPointComponent>();
        Check(unit.Skills.TryQueue(100102, out _), "preparation selection");
        Check(unit.Skills.CooldownRemaining(100102) == 0, "selection does not start cooldown");
        Check(unit.Skills.CancelPendingSkill() == 1 && unit.Skills.CooldownRemaining(100102) == 0, "cancel refunds with no cooldown");
        battle.Round.Phase = BattlePhase.Combat;
        battle.Round.ActionRemaining = 100;
        Cast(unit, 100102);
        Check(unit.Skills.CooldownRemaining(100102) == 2, "CD two starts after successful cast in round one");
        int points = pool.Current;
        Check(!unit.Skills.TryQueue(100102, out string reason) && reason.Contains("冷却") && pool.Current == points,
            "cooldown blocks selection without spending points");
        Check(unit.Skills.CooldownRemaining(100101) == 0 && friend.Skills.CooldownRemaining(100102) == 0 &&
            other.GetUnitAt(9).Skills.CooldownRemaining(100102) == 0, "cooldown belongs to this skill, unit and battle");
        battle.World.Update(10);
        Check(unit.Skills.CooldownRemaining(100102) == 2, "seconds elapsed do not decrement round cooldown");
        battle.Round.RoundNumber = 2;
        pool.BeginRound(2);
        Check(unit.Skills.CooldownRemaining(100102) == 1 && !unit.Skills.CanQueue(100102, out _), "round two still cooling");
        battle.Round.RoundNumber = 3;
        pool.BeginRound(3);
        Check(unit.Skills.CooldownRemaining(100102) == 0 && unit.Skills.CanQueue(100102, out _), "round three ready without waiting seconds");
        Cast(unit, 100102);
        Check(unit.Skills.CooldownRemaining(100102) == 2, "recasting restarts full cooldown");

        // Range failure is checked at execution after reservation and must refund without starting CD.
        BattleUnit rear = battle.GetUnitAt(3);
        Check(rear.Skills.TryQueue(100101, out _), "reserve out of range attack");
        int reserved = pool.Current;
        Check(!rear.Skills.TryExecutePending(new BattleActionResult()) && rear.Skills.CooldownRemaining(100101) == 0 && pool.Current == reserved + 1,
            "failed cast refunds and has no cooldown");
        BattleUnit enemy = battle.GetUnitAt(12);
        enemy.Skills.CancelPendingSkill();
        Cast(enemy, 100102);
        battle.GetComponent<BattleEnemyAIComponent>().TrySelectSkill(enemy);
        Check(enemy.Skills.PendingSkillId != 100102 && enemy.Skills.CooldownRemaining(100102) == 2,
            "enemy AI excludes cooling skills");

        using var scheduled = Create(2);
        BattleUnit actor = scheduled.GetUnitAt(9);
        foreach (BattleUnit member in scheduled.Battlefield.Units)
            if (member != actor) member.RemoveComponent<BattleActionComponent>();
        Check(actor.Skills.TryQueue(100102, out _), "reserve before scheduled action");
        scheduled.World.Update(16);
        Check(scheduled.Round.RoundNumber == 2 && actor.Skills.CooldownRemaining(100102) == 1, "round transition decrements CD once");
        scheduled.Enabled = false;
        scheduled.World.Update(100);
        Check(actor.Skills.CooldownRemaining(100102) == 1, "paused battle keeps cooldown");
        scheduled.Enabled = true;
        scheduled.World.Update(15);
        Check(actor.Skills.CooldownRemaining(100102) == 1 && !actor.Skills.TryQueue(100102, out _), "preparation timeout does not decrement CD");
        scheduled.World.Update(1);
        Check(scheduled.Round.RoundNumber == 3 && actor.Skills.CooldownRemaining(100102) == 0 && actor.Skills.CanQueue(100102, out _),
            "two real round transitions restore the skill");

        using var noCd = Create(0);
        noCd.Round.Phase = BattlePhase.Combat;
        Cast(noCd.GetUnitAt(9), 100102);
        Check(noCd.GetUnitAt(9).Skills.CooldownRemaining(100102) == 0, "zero CD means no cooldown");
        using var oneRound = Create(1);
        oneRound.Round.Phase = BattlePhase.Combat;
        Cast(oneRound.GetUnitAt(9), 100102);
        oneRound.Round.RoundNumber = 2;
        Check(oneRound.GetUnitAt(9).Skills.CooldownRemaining(100102) == 0, "CD one is ready next round");
        try { using var invalid = Create(-1); throw new Exception("negative CD accepted"); }
        catch (ArgumentException) { }
        Console.WriteLine("PASS: round-based skill CD CSV/snapshots, cast-only start, per-skill/unit/battle isolation, UI/AI eligibility, no point loss while cooling, failure/cancel refunds, round transitions, elapsed seconds/pause, zero and invalid CD.");
    }
}
