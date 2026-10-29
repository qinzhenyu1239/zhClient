using System;
using System.Collections.Generic;
using System.Linq;

static class MoonChecks
{
    sealed class FirstRandom : Random
    {
        public override int Next(int maxValue) => 0;
        public override double NextDouble() => 0;
    }

    static void Check(bool value, string message)
    {
        if (!value) throw new Exception("Moon: " + message);
    }
    static void Near(double actual, double expected, string message) =>
        Check(Math.Abs(actual - expected) < 0.001, message + ": " + actual);

    static BattleEntity Create(int moonSlot = 3)
    {
        var wolf = new character { ID = 1001, name = "fixture", hp = 10000, attack = 100,
            defense = 0, speed = 100, attackRange = 1, battleType = 1 };
        var moon = new character { ID = 1003, name = "月光女神", hp = 10000, attack = 100,
            defense = 0, speed = 100, attackRange = 1, battleType = 3,
            skill1Id = 100301, skill2Id = 100302, skill3Id = 100303 };
        return BattleInitializationSystem.CreateBattle(wolf, random: new FirstRandom(),
            skillConfigs: new[]
            {
                new skill { ID = 100301, param1 = "100", skillRange = 99, skillPointCost = 5 },
                new skill { ID = 100302, param1 = "100", skillRange = 1, skillPointCost = 5 },
                new skill { ID = 100303, param1 = "1", param2 = "0.1", param3 = "2", param4 = "200", param5 = "3", skillRange = 99, skillPointCost = 10 }
            }, // Fixed damage fixtures stay independent of editable balance/CD values; CheckTables verifies the live table.
            lineup: new Dictionary<int, character> { { moonSlot, moon } });
    }

    static BattleActionResult Cast(BattleUnit unit, int id)
    {
        var battle = unit.GetParent<BattleEntity>();
        battle.Round.Phase = BattlePhase.Combat;
        battle.Round.ActionRemaining = 100;
        Check(unit.Skills.TryQueue(id, out string reason), "queue " + id + ": " + reason);
        var action = new BattleActionResult { Unit = unit, FromSlot = unit.SlotIndex, ToSlot = unit.SlotIndex };
        Check(unit.Skills.TryExecutePending(action), "execute " + id);
        return action;
    }

    static void CheckTables()
    {
        var config = ExcelDataMgr.Instance.GetConfigData<character>("1003");
        Check(config.name == "月光女神" && config.job == "刺客" && config.battleType == 3 &&
            config.hp == 850 && config.attack == 140 && config.defense == 10 && config.speed == 100 && config.attackRange == 1,
            "live character stats and assassin class");
        var records = ExcelDataMgr.Instance.GetConfigDatas<skill>(null);
        using var battle = BattleInitializationSystem.CreateRandomBattle(new[] { config }, random: new FirstRandom(), skillConfigs: records);
        Check(battle.Battlefield.Units.Count == 2, "single hero candidate produces one unit on each side");
        Check(battle.Battlefield.Units.All(unit => unit.GetComponent<BattleAssassinComponent>() != null), "all assassin units have movement component");
        var skills = battle.GetUnitAt(3).Skills;
        Check(skills.GetDefinition(100301).PointCost == 5 && skills.GetDefinition(100302).PointCost == 5 &&
            skills.GetDefinition(100303).PointCost == 10 && skills.GetDefinition(100303).MoonTickCount() == 10 &&
            skills.GetDefinition(100303).Int(5) == 3,
            "live skill cost and 1-second/0.1-second configuration");
        Check(!skills.GetDefinition(100303).FormatDescription().Contains("param"), "moon skill description substitutes all parameters");
    }

    static void CheckAssassins()
    {
        foreach (int slot in new[] { 3, 12 })
        {
            using var battle = Create(slot);
            BattleUnit unit = battle.GetUnitAt(slot);
            int home = slot == 3 ? 3 : 18;
            int rear = slot == 3 ? 21 : 0;
            var assassin = unit.GetComponent<BattleAssassinComponent>();
            if (battle.GetUnitAt(home) != unit) battle.GetUnitAt(home).Health.ApplyDamage(10000);
            Check(!unit.Position.TryPlace(slot == 3 ? 0 : 21), "cannot enter own reserved edge column");
            Check(unit.Position.TryPlace(home), "place at column two or seven");
            battle.World.Update(15);
            Check(unit.SlotIndex == home && !assassin.HasInfiltrated, "combat start never jumps before movement turn");
            Check(unit.GetComponent<BattleMovementComponent>().TryMoveForward() && unit.SlotIndex == rear && assassin.HasInfiltrated,
                "next movement jumps from column two or seven to opposing rear");
            Check(unit.ForwardStep() == (slot == 3 ? -3 : 3), "infiltrated assassin faces inward");
        }
        foreach (int slot in new[] { 3, 12 })
        {
            using var battle = Create(slot);
            BattleUnit actor = battle.GetUnitAt(slot);
            int home = slot == 3 ? 3 : 18;
            if (battle.GetUnitAt(home) != actor) battle.GetUnitAt(home).Health.ApplyDamage(10000);
            Check(actor.Position.TryPlace(home), "prepare assassin at jump boundary");
            BattleUnit target = battle.Battlefield.Units.First(unit => unit.Side != actor.Side && unit.IsAlive);
            Check(target.Position.TryPlace(slot == 3 ? 0 : 21), "enemy occupies adjacent reserved edge");
            Check(!actor.GetComponent<BattleMovementComponent>().TryMoveForward() && actor.SlotIndex == home,
                "adjacent enemy prevents both retreat and jump for either side");
            battle.World.Update(15);
            battle.Round.ActionOrder.Remove(actor);
            battle.Round.ActionOrder.Insert(0, actor);
            battle.World.Update(1);
            var result = battle.Round.DrainActions().Single();
            Check(result.Unit == actor && result.FromSlot == home && result.ToSlot == home &&
                !actor.GetComponent<BattleAssassinComponent>().HasInfiltrated,
                "scheduled action searches for targets before any assassin movement");
        }
        using (var battle = Create())
        {
            BattleUnit actor = battle.GetUnitAt(3);
            Check(actor.Skills.TryQueue(100301, out _), "reserve cleave before movement turn");
            battle.World.Update(15);
            battle.Round.ActionOrder.Remove(actor);
            battle.Round.ActionOrder.Insert(0, actor);
            battle.World.Update(1);
            var result = battle.Round.DrainActions().Single();
            Check(result.FromSlot == 3 && result.ToSlot == 21 && result.Kind == BattleActionKind.Skill &&
                result.Hits.Count == 3 && actor.HasActedThisRound,
                "jump is the one movement then row skill resolves once at destination");
        }
        using (var battle = Create())
        {
            BattleUnit unit = battle.GetUnitAt(3);
            battle.GetUnitAt(9).Health.ApplyDamage(10000);
            Check(unit.Position.TryPlace(9), "start away from rear");
            battle.GetUnitAt(3)?.Health.ApplyDamage(10000);
            battle.GetUnitAt(6).Health.ApplyDamage(10000);
            battle.GetUnitAt(12).Health.ApplyDamage(10000);
            Check(unit.GetComponent<BattleMovementComponent>().TryMoveForward() && unit.SlotIndex == 6, "retreat one column");
            Check(unit.GetComponent<BattleMovementComponent>().TryMoveForward() && unit.SlotIndex == 3 &&
                !unit.GetComponent<BattleAssassinComponent>().HasInfiltrated, "reaching column two uses one movement without an extra jump");
            Check(unit.GetComponent<BattleMovementComponent>().TryMoveForward() && unit.SlotIndex == 21, "reach own rear then jump");
            Check(unit.FindAdjacentEnemy() == battle.GetUnitAt(18), "backline target is now forward");
            battle.GetUnitAt(18).Health.ApplyDamage(10000);
            Check(unit.GetComponent<BattleMovementComponent>().TryMoveForward() && unit.SlotIndex == 18, "advance inward after infiltration");
        }
        using (var battle = Create())
        {
            BattleUnit unit = battle.GetUnitAt(3);
            for (int row = 0; row < 3; row++) Check(battle.GetUnitAt(6 + row).Position.TryPlace(21 + row), "block enemy rear with legal allied occupants");
            Check(!unit.GetComponent<BattleMovementComponent>().TryMoveForward() && unit.SlotIndex == 3,
                "full opposing rear waits without overwriting occupants");
            battle.GetUnitAt(21).Health.ApplyDamage(10000);
            Check(unit.GetComponent<BattleMovementComponent>().TryMoveForward() && unit.SlotIndex == 21, "retry when opposing rear opens");
        }
    }

    static void CheckCleaveAndMarks()
    {
        using var battle = Create();
        BattleUnit unit = battle.GetUnitAt(3);
        var action = Cast(unit, 100301);
        Check(action.Hits.Count == 3 && action.MoonMarkedSlots.Count == 3 &&
            action.Hits.Select(hit => hit.TargetSlot).OrderBy(slot => slot).SequenceEqual(new[] { 12, 15, 18 }),
            "cleave attacks every enemy in same horizontal row only");
        foreach (int slot in new[] { 12, 15, 18 })
            Check(battle.GetUnitAt(slot).CurrentHp == 9900 && battle.GetUnitAt(slot).GetComponent<BattleMoonMarkComponent>().Stacks == 1,
                "cleave deals one attack and one mark");
        action = Cast(unit, 100301);
        Check(action.MoonExplosions == 3 && action.Hits.Count == 6, "second mark detonates each enemy once");
        foreach (int slot in new[] { 12, 15, 18 })
            Check(battle.GetUnitAt(slot).CurrentHp == 9600 && battle.GetUnitAt(slot).GetComponent<BattleMoonMarkComponent>() == null,
                "detonation adds 200% attack damage and consumes marks");
        Check(battle.GetUnitAt(13).CurrentHp == 10000, "detonation affects marked enemy only");
        BattleUnit target = battle.GetUnitAt(12);
        target.Combat.Defense = 20;
        var isolated = new BattleActionResult();
        unit.ApplyMoonMark(target, isolated);
        target.AddShield(100, 2);
        unit.ApplyMoonMark(target, isolated);
        Near(target.CurrentHp, 9520, "200% attack minus armor then shield");
        Check(isolated.MoonExplosions == 1 && target.GetComponent<BattleMoonMarkComponent>() == null, "marks reset after explosion");
        unit.ApplyMoonMark(target, isolated);
        target.Health.ApplyDamage(100000);
        Check(target.GetComponent<BattleMoonMarkComponent>() == null, "death clears marks");
    }

    static void CheckBlink()
    {
        using var battle = Create();
        BattleUnit unit = battle.GetUnitAt(3);
        battle.GetUnitAt(4).Health.ApplyDamage(10000);
        battle.GetUnitAt(5).Health.ApplyDamage(10000);
        battle.GetUnitAt(8).Health.ApplyDamage(10000);
        Check(battle.GetUnitAt(12).Position.TryPlace(5), "place adjacent enemy at destination");
        Check(battle.GetUnitAt(13).Position.TryPlace(8), "place nonadjacent enemy");
        Check(unit.EmptyMoonSlots().All(slot => slot >= 3), "ally blink never selects column one");
        Check(battle.GetUnitAt(5).EmptyMoonSlots().All(slot => slot < 21), "enemy blink never selects column eight");
        var action = Cast(unit, 100302); // FirstRandom selects legal empty slot four.
        Check(unit.SlotIndex == 4 && battle.GetUnitAt(3) == null && battle.GetUnitAt(4) == unit && action.ToSlot == 4,
            "blink chooses empty map position and updates occupancy and result");
        Check(action.Hits.Count == 1 && action.Hits[0].TargetSlot == 5 && action.MoonMarkedSlots.SequenceEqual(new[] { 5 }),
            "blink attacks only adjacent enemy without column wrapping");
        Check(battle.GetUnitAt(8).CurrentHp == 10000, "blink excludes nonadjacent enemy");
        Check(battle.GetUnitAt(5).CurrentHp == 9900, "blink deals ordinary attack damage");
    }

    static void CheckShower()
    {
        using var big = Create();
        using var small = Create();
        Cast(big.GetUnitAt(3), 100303);
        Cast(small.GetUnitAt(3), 100303);
        big.World.Update(0.05);
        Check(big.Round.DrainActions().Count == 0, "no immediate mark before 0.1 seconds");
        big.World.Update(0.05);
        var first = big.Round.DrainActions();
        Check(first.Count == 1 && first[0].MoonMarkedSlots.Count == 1 && first[0].Hits.Count == 0, "first tick is mark only");
        big.World.Update(0.9);
        var rest = big.Round.DrainActions();
        Check(rest.Count == 9 && rest.Sum(action => action.MoonExplosions) == 3, "ten marks spread across targets with three detonations");
        Near(big.GetUnitAt(12).CurrentHp, 9800, "first enemy receives at most three marks and one detonation");
        var applications = first.Concat(rest).SelectMany(action => action.MoonMarkedSlots).GroupBy(slot => slot).ToArray();
        Check(applications.Sum(group => group.Count()) == 10 && applications.All(group => group.Count() <= 3),
            "saturated enemies are excluded and remaining marks redirect to other enemies");
        for (int i = 0; i < 100; i++) small.World.Update(0.01);
        Check(small.Round.DrainActions().Count == 10 && small.GetUnitAt(12).CurrentHp == big.GetUnitAt(12).CurrentHp,
            "large and small frames produce identical timing and damage");
        Check(big.GetUnitAt(3).GetComponent<BattleMoonShowerComponent>() == null, "shower expires after ten marks");
        big.World.Update(1);
        Check(big.Round.DrainActions().Count == 0, "no extra ticks after one second");
        using var paused = Create();
        BattleUnit caster = paused.GetUnitAt(3);
        Cast(caster, 100303);
        paused.Enabled = false;
        paused.World.Update(1);
        Check(paused.Round.DrainActions().Count == 0, "disabled battle pauses shower");
        paused.Enabled = true;
        paused.World.Update(0.1);
        Check(paused.Round.DrainActions().Count == 1, "shower resumes with battle");
        caster.Health.ApplyDamage(caster.MaxHp);
        paused.World.Update(1);
        Check(paused.Round.DrainActions().Count == 0 && caster.GetComponent<BattleMoonShowerComponent>() == null, "dead caster stops shower");
        using var removed = Create();
        Cast(removed.GetUnitAt(3), 100303);
        removed.RemoveComponent<BattleRoundComponent>();
        Check(removed.GetUnitAt(3).GetComponent<BattleMoonShowerComponent>() == null, "removing round cancels shower");

        using var victory = Create();
        BattleUnit winner = victory.GetUnitAt(3);
        foreach (BattleUnit enemy in victory.Battlefield.Units.Where(unit => unit.Side == BattleSide.Enemy && unit.SlotIndex != 12))
            enemy.Health.ApplyDamage(enemy.MaxHp);
        victory.GetUnitAt(12).Health.CurrentHp = 1;
        Cast(winner, 100303);
        victory.World.Update(1);
        Check(victory.Round.Phase == BattlePhase.Finished && victory.Round.DrainActions().Count == 2 &&
            winner.GetComponent<BattleMoonShowerComponent>() == null, "victory on second mark cancels all remaining ticks");

        using var scheduled = Create();
        BattleUnit actor = scheduled.GetUnitAt(3);
        foreach (BattleUnit unit in scheduled.Battlefield.Units)
            if (unit != actor) unit.RemoveComponent<BattleActionComponent>();
        Check(actor.Skills.TryQueue(100303, out _), "reserve moon shower during preparation");
        scheduled.World.Update(16);
        var cast = scheduled.Round.DrainActions();
        Check(cast.Count == 1 && cast[0].Kind == BattleActionKind.Skill && scheduled.Round.RoundNumber == 2,
            "cast consumes one action with no retroactive ticks and starts next preparation");
        scheduled.World.Update(1);
        Check(scheduled.Round.DrainActions().Count == 10 && scheduled.Round.PreparationRemaining == 14,
            "all ten real-time marks continue across next preparation without new actions");
        Check(scheduled.GetComponent<BattleSkillPointComponent>().Current == 30, "timed marks never spend next round points");
    }

    static void CheckShowerPerEnemyCap()
    {
        using var battle = Create();
        BattleUnit caster = battle.GetUnitAt(3);
        BattleUnit target = battle.GetUnitAt(12);
        foreach (BattleUnit enemy in battle.Battlefield.Units.Where(unit => unit.Side == BattleSide.Enemy && unit != target))
            enemy.Health.ApplyDamage(enemy.MaxHp);
        Cast(caster, 100303);
        battle.World.Update(0.1);
        var first = battle.Round.DrainActions();
        Check(target.Position.TryPlace(0), "marked enemy moves to another slot during shower");
        battle.World.Update(0.9);
        var rest = battle.Round.DrainActions();
        Check(first.Count + rest.Count == 3 && rest.Sum(action => action.MoonExplosions) == 1,
            "single enemy receives only three cumulative marks despite detonation and movement");
        Near(target.CurrentHp, 9800, "single target cap prevents repeat explosions in same cast");
        Check(target.GetComponent<BattleMoonMarkComponent>().Stacks == 1 && caster.GetComponent<BattleMoonShowerComponent>() == null,
            "one mark remains and shower stops when no eligible enemy remains");
        battle.GetComponent<BattleSkillPointComponent>().BeginRound(2);
        Cast(caster, 100303);
        battle.World.Update(1);
        var recast = battle.Round.DrainActions();
        Check(recast.Count == 3 && recast.Sum(action => action.MoonExplosions) == 2,
            "new cast resets application quota and works with preexisting mark");
    }

    public static void Run()
    {
        CheckTables();
        CheckAssassins();
        CheckCleaveAndMarks();
        CheckBlink();
        CheckShower();
        CheckShowerPerEnemyCap();
        Console.WriteLine("PASS: Moon live CSV stats/costs, assassin mirrored rear jump/retreat/retry/inward attack, row cleave, empty-cell blink, moon mark stacking/detonation/armor/shield/death, ten 0.1-second ticks, frame partition, pause/death/disposal.");
    }
}
