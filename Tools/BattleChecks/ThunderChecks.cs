using System;
using System.Linq;
using System.Collections.Generic;

static class ThunderChecks
{
    sealed class FixedRandom : Random
    {
        public override int Next(int maxValue) => 0;
        public override double NextDouble() => 0;
    }
    sealed class AlternatingRandom : Random
    {
        public int Draws { get; private set; }
        public override int Next(int maxValue) => Draws++ % maxValue;
    }

    static void CheckRandomFormation()
    {
        var wolf = ExcelDataMgr.Instance.GetConfigData<character>("1001");
        var thor = ExcelDataMgr.Instance.GetConfigData<character>("1002");
        var candidates = new[] { wolf, thor };
        var records = ExcelDataMgr.Instance.GetConfigDatas<skill>(null);
        var random = new AlternatingRandom();
        using var battle = BattleInitializationSystem.CreateRandomBattle(candidates, random: random, skillConfigs: records);
        Check(random.Draws >= 18 && battle.Battlefield.Units.Count == 18, "each of 18 occupied slots draws independently before AI planning");
        foreach (var unit in battle.Battlefield.Units)
            Check(unit.CharacterId == candidates[(unit.SlotIndex - 9) % 2].ID, "formation draw order is independent of later AI choices");
        foreach (var side in new[] { BattleSide.Ally, BattleSide.Enemy })
        {
            var units = battle.Battlefield.Units.Where(unit => unit.Side == side).ToArray();
            Check(units.Length == 9 && units.Select(unit => unit.CharacterId).Distinct().Count() == 2,
                "both sides accept both random heroes");
            foreach (var unit in units)
            {
                var config = unit.CharacterId == wolf.ID ? wolf : thor;
                Check(unit.MaxHp == config.hp && unit.Attack == config.attack && unit.Defense == config.defense &&
                    unit.Skill1Id == config.skill1Id && unit.Skill2Id == config.skill2Id && unit.Skill3Id == config.skill3Id,
                    "random hero retains its own stats and skills");
            }
        }
        Check(Enumerable.Range(0, 9).All(slot => battle.GetUnitAt(slot) == null), "random formation preserves rear empty cells");
        using var first = BattleInitializationSystem.CreateRandomBattle(candidates, random: new Random(42), skillConfigs: records);
        using var second = BattleInitializationSystem.CreateRandomBattle(candidates, random: new Random(42), skillConfigs: records);
        Check(first.Battlefield.Units.Select(unit => unit.CharacterId).SequenceEqual(second.Battlefield.Units.Select(unit => unit.CharacterId)),
            "seeded random formation is reproducible");
    }
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Thunder: " + message);
    }
    static void Near(float actual, float expected, string message) => Check(Math.Abs(actual - expected) < .01f, message + " actual=" + actual);
    static BattleEntity Create()
    {
        // Engine scenarios use stable fixtures; CheckRandomFormation above verifies the live CSV snapshots.
        var wolf = new character { ID = 1001, name = "血狼人", hp = 1200, attack = 100, defense = 20,
            speed = 88, attackRange = 1, skill1Id = 100101, skill2Id = 100102, skill3Id = 100103, battleType = 1 };
        var thor = new character { ID = 1002, name = "雷神", hp = 1400, attack = 130, defense = 30,
            speed = 75, attackRange = 2, skill1Id = 100201, skill2Id = 100202, skill3Id = 100203, battleType = 2 };
        var records = new[] {
            new skill { ID = 100101, param1 = "2", param2 = "2", param3 = "10", skillRange = 1, skillPointCost = 2 },
            new skill { ID = 100102, param1 = "10", param2 = "5", skillRange = 3, skillPointCost = 2 },
            new skill { ID = 100103, param1 = "3", param2 = "1", param3 = "5", skillRange = 1, skillPointCost = 5 },
            new skill { ID = 100201, param1 = "150", param2 = "1", skillRange = 99, skillPointCost = 2 },
            new skill { ID = 100202, param1 = "3", param2 = "8", param3 = "2", skillRange = 99, skillPointCost = 2 },
            new skill { ID = 100203, param1 = "4", param2 = "200", param3 = "400", param4 = "2", skillRange = 99, skillPointCost = 5 }
        };
        return BattleInitializationSystem.CreateBattle(wolf, random: new FixedRandom(),
            skillConfigs: records,
            lineup: new Dictionary<int, character> { { 15, thor } });
    }
    static BattleActionResult Cast(BattleUnit unit, int skill)
    {
        Check(unit.Skills.TryQueue(skill, out string reason), "queue " + skill + " " + reason);
        var action = new BattleActionResult { Unit = unit, FromSlot = unit.SlotIndex, ToSlot = unit.SlotIndex };
        Check(unit.Skills.TryExecutePending(action), "execute " + skill);
        return action;
    }
    static float Shield(BattleUnit unit) => unit.GetComponent<BattleShieldComponent>()?.Amount ?? 0;
    static void CheckSilence()
    {
        using var battle = Create();
        var unit = battle.GetUnitAt(15);
        var target = battle.GetUnitAt(18);
        Check(unit.CharacterId == 1002 && unit.Name == "雷神" && target.CharacterId == 1001, "mixed formation identity");
        Check(unit.MaxHp == 1400 && unit.Attack == 130 && unit.Defense == 30 && unit.Speed == 75 && unit.AttackRange == 2, "Thor base configuration");
        battle.Round.Phase = BattlePhase.Combat;
        var strike = Cast(unit, BattleSkillDefinition.ThunderStrike);
        Near(strike.Damage, Math.Max(0, 195 - target.Defense), "150 percent attack before defense");
        Check(!target.IsSilenced(), "silence starts next round, not cast round");
        battle.Round.RoundNumber = 2;
        Check(target.IsSilenced(), "next round silenced");
        var blocked = new BattleActionResult();
        target.GetComponent<BattleBasicAttackComponent>().TryAttack(blocked);
        Check(blocked.Hits.Count == 0 && blocked.Message.Contains("沉默"), "silence blocks basic attack");
        // The same debuff also prevents an allied hero from reserving any skill points.
        var silence = unit.AddComponent<BattleSilenceComponent>();
        silence.FirstRound = silence.LastRound = 2;
        int points = battle.GetComponent<BattleSkillPointComponent>().Current;
        Check(!unit.Skills.TryQueue(BattleSkillDefinition.ChainLightning, out _) && points == battle.GetComponent<BattleSkillPointComponent>().Current,
            "silence blocks skills without spending points");
        battle.Round.RoundNumber = 3;
        silence.OnRoundStarted(3);
        target.GetComponent<BattleSilenceComponent>().OnRoundStarted(3);
        Check(!target.IsSilenced() && !unit.IsSilenced(), "silence expires after one round");
        Check(unit.Skills.TryQueue(BattleSkillDefinition.ChainLightning, out _), "skills restored after silence");
    }
    static void CheckChainAndShield()
    {
        using var battle = Create();
        battle.Round.Phase = BattlePhase.Combat;
        var unit = battle.GetUnitAt(15);
        var expected = unit.Combat.FindEnemies(99, 3);
        Check(expected.Select(u => u.SlotIndex).SequenceEqual(new[] { 18, 19, 21 }), "nearest three with stable tie order");
        expected[0].Health.CurrentHp = 10;
        foreach (var enemy in expected) enemy.Combat.Defense = 9999;
        float perTarget = expected[1].MaxHp * .08f;
        var chain = Cast(unit, BattleSkillDefinition.ChainLightning);
        Check(chain.Hits.Count == 3 && chain.Hits.Select(h => h.TargetSlot).Distinct().Count() == 3, "three distinct enemies");
        Near(chain.Damage, 10 + 2 * perTarget, "eight percent max HP ignores defense and clamps overkill");
        Near(Shield(unit), chain.Damage, "actual HP damage becomes shield");
        float hp = unit.CurrentHp;
        Near(unit.Health.ApplyDamage(30), 0, "shield absorbs before HP");
        Near(unit.CurrentHp, hp, "shield preserves HP");
        float firstShield = Shield(unit);
        battle.Round.RoundNumber = 2;
        unit.AddShield(100, 2);
        Near(Shield(unit), firstShield + 100, "shields stack");
        unit.GetComponent<BattleShieldComponent>().OnRoundStarted(3);
        Near(Shield(unit), 100, "old layer expires without removing newer layer");
        unit.GetComponent<BattleShieldComponent>().OnRoundStarted(4);
        Near(Shield(unit), 0, "new layer independently expires");
        unit.AddShield(20, 2);
        Near(unit.Health.ApplyDamage(25), 5, "damage exceeding shield reaches HP");
        Near(unit.CurrentHp, hp - 5, "exact shield overflow");
    }
    static BattleEntity CloudBattle()
    {
        var battle = Create();
        battle.Round.Phase = BattlePhase.Combat;
        battle.Round.ActionRemaining = 100;
        var unit = battle.GetUnitAt(15);
        unit.AddShield(50, 2);
        battle.GetUnitAt(18).Health.CurrentHp = 1;
        Cast(unit, BattleSkillDefinition.Thundercloud);
        return battle;
    }
    static void CheckCloud()
    {
        using var battle = CloudBattle();
        var unit = battle.GetUnitAt(15);
        battle.World.Update(.5);
        Check(battle.Round.DrainActions().Count == 0, "no immediate or half-second strike");
        battle.World.Update(.5);
        var first = battle.Round.DrainActions().Single();
        Check(first.Kind == BattleActionKind.Lightning && first.Hits.Count == 1 && !first.Target.IsAlive, "one second lightning kills");
        Near(first.ShieldGained, 520, "kill shield equals four times Thor attack");
        Near(Shield(unit), 570, "kill shield stacks with existing shield");
        battle.Enabled = false;
        battle.World.Update(10);
        Check(battle.Round.DrainActions().Count == 0, "disabled battle pauses cloud");
        battle.Enabled = true;
        battle.World.Update(3);
        Check(battle.Round.DrainActions().Count == 3 && unit.GetComponent<BattleThundercloudComponent>() == null, "four total strikes then expiry");
        battle.World.Update(1);
        Check(battle.Round.DrainActions().Count == 0, "no fifth strike");
        using var largeFrame = CloudBattle();
        using var smallFrames = CloudBattle();
        largeFrame.World.Update(4);
        for (int i = 0; i < 4; i++) smallFrames.World.Update(1);
        Check(largeFrame.Round.DrainActions().Count == 4 && smallFrames.Round.DrainActions().Count == 4, "long frame catches up all four strikes");
        Check(largeFrame.Battlefield.Units.Select(u => u.CurrentHp).SequenceEqual(smallFrames.Battlefield.Units.Select(u => u.CurrentHp)), "partition-independent lightning damage");
        using var deadCaster = CloudBattle();
        var caster = deadCaster.GetUnitAt(15);
        caster.Health.ApplyDamage(100000);
        deadCaster.World.Update(4);
        Check(deadCaster.Round.DrainActions().Count == 0 && caster.GetComponent<BattleThundercloudComponent>() == null, "dead caster stops cloud");
        using var victory = CloudBattle();
        foreach (var enemy in victory.Battlefield.Units.Where(u => u.Side == BattleSide.Enemy && u.SlotIndex != 18))
            enemy.Health.ApplyDamage(enemy.MaxHp);
        victory.World.Update(4);
        Check(victory.Round.Phase == BattlePhase.Finished && victory.Round.DrainActions().Count == 1 &&
            victory.GetUnitAt(15).GetComponent<BattleThundercloudComponent>() == null, "victory stops all later cloud strikes");
        using var removedRound = CloudBattle();
        var remainingCaster = removedRound.GetUnitAt(15);
        removedRound.RemoveComponent<BattleRoundComponent>();
        Check(remainingCaster.GetComponent<BattleThundercloudComponent>() == null, "round removal cancels lingering cloud");
    }
    static void CheckScheduledCloud()
    {
        using var battle = Create();
        var actor = battle.GetUnitAt(15);
        foreach (var unit in battle.Battlefield.Units)
            if (unit != actor) unit.RemoveComponent<BattleActionComponent>();
        Check(actor.Skills.TryQueue(BattleSkillDefinition.Thundercloud, out _), "reserve cloud during preparation");
        battle.World.Update(16);
        Check(battle.Round.DrainActions().Count == 1 && battle.Round.RoundNumber == 2, "cast consumes action, no retroactive strikes");
        battle.World.Update(4);
        Check(battle.Round.DrainActions().Count == 4 && battle.Round.PreparationRemaining == 11,
            "cloud continues for four actual seconds across round preparation");
        var points = battle.GetComponent<BattleSkillPointComponent>();
        Check(points.Current == 30, "cloud ticks do not consume next round skill budget");
    }
    public static void Run()
    {
        CheckRandomFormation();
        CheckSilence();
        CheckChainAndShield();
        CheckCloud();
        CheckScheduledCloud();
        Console.WriteLine("PASS: Thor config/mixed formation, 150% strike, next-round silence, nearest-three 8% lightning, actual-damage shields, independent shield expiry/overflow, four real-time cloud ticks, kill shields, pause/death, frame partition and round transitions.");
    }
}
