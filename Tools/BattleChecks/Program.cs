using Framework.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class Program
{
    static IReadOnlyList<BattleActionResult> Tick(BattleEntity battle, double seconds, Random random)
    {
        if (battle.Round != null) battle.Round.Random = random;
        battle.World.Update(seconds);
        battle.World.LateUpdate();
        return battle.Round.DrainActions();
    }

    static bool TryMove(BattleEntity battle, BattleUnit unit) => unit.GetComponent<BattleMovementComponent>().TryMoveTowardEnemy();
    static BattleUnit FindTarget(BattleEntity battle, BattleUnit unit) => unit.GetComponent<BattleBasicAttackComponent>().FindTargetInRange();
    static float ApplyDamage(BattleEntity battle, BattleUnit unit, float damage) => unit.Health.ApplyDamage(damage);

    sealed class KeepOrderRandom : Random
    {
        public override int Next(int maxValue) => maxValue - 1;
    }

    static character Config(float range = 1, int hp = 1000, int attack = 100, int defense = 100)
    {
        return new character { ID = 1001, name = "血狼人", hp = hp, attack = attack,
            defense = defense, speed = 88, attackRange = range, skill1Id = 100101,
            skill2Id = 100102, skill3Id = 100103, battleType = 1 };
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static void Near(double actual, double expected, string message)
    {
        Check(Math.Abs(actual - expected) < 0.001, message + ": " + actual);
    }

    static void RemoveExcept(BattleEntity battle, params int[] slots)
    {
        foreach (BattleUnit unit in battle.Battlefield.Units)
            if (!slots.Contains(unit.SlotIndex)) ApplyDamage(battle, unit, unit.MaxHp);
    }

    static void CheckClockAndOrder()
    {
        var battle = BattleInitializationSystem.CreateBattle(Config(0));
        BattleRoundComponent turns = battle.Round;
        var random = new Random(42);
        var actions = new List<BattleActionResult>();
        actions.AddRange(Tick(battle, 14, random));
        Check(turns.Phase == BattlePhase.Preparation && actions.Count == 0, "No preparation actions");
        Near(turns.PreparationRemaining, 1, "15-second preparation");
        actions.AddRange(Tick(battle, 1, random));
        Check(turns.Phase == BattlePhase.Combat && actions.Count == 0, "Combat starts after preparation");
        actions.AddRange(Tick(battle, 0.5, random));
        Check(actions.Count == 0, "No action before a full second");
        actions.AddRange(Tick(battle, 0.5, random));
        Check(actions.Count == 1 && actions[0].Unit.HasActedThisRound, "Exactly one action per second");
        actions.AddRange(Tick(battle, 17, random));
        Check(actions.Count == 18 && actions.Select(a => a.Unit.UnitId).Distinct().Count() == 18, "Every unit acts once");
        Check(actions.All(a => a.Target == null && a.Damage == 0), "Zero range cannot attack");
        Check(turns.RoundNumber == 2 && turns.Phase == BattlePhase.Preparation, "Next round returns to preparation");
        Near(turns.PreparationRemaining, 15, "Fresh preparation timer");
        Check(battle.Battlefield.Units.All(unit => !unit.HasActedThisRound), "Action flags reset next round");
        Check(!actions.Select(a => a.Unit.UnitId).SequenceEqual(battle.Battlefield.Units.Select(u => u.UnitId)), "Order is shuffled");
        actions.AddRange(Tick(battle, 33, random));
        Check(turns.RoundNumber == 3 && actions.Count == 36, "Repeated complete rounds");
        Check(actions.GroupBy(a => a.RoundNumber).All(g => g.Count() == 18 && g.Select(a => a.Unit.UnitId).Distinct().Count() == 18), "One action per unit per round");
        Check(!actions.Take(18).Select(a => a.Unit.UnitId).SequenceEqual(actions.Skip(18).Select(a => a.Unit.UnitId)), "Shuffle again each round");

        BattleEntity bigStep = BattleInitializationSystem.CreateBattle(Config(0));
        int bigStepActions = Tick(bigStep, 66, new Random(42)).Count;
        Check(bigStep.Round.RoundNumber == turns.RoundNumber && bigStep.Round.Phase == turns.Phase && bigStepActions == actions.Count, "Frame stalls preserve elapsed time");
        var deadAtStart = BattleInitializationSystem.CreateBattle(Config(0));
        ApplyDamage(deadAtStart, deadAtStart.GetUnitAt(3), 1000);
        IReadOnlyList<BattleActionResult> skipActions = Tick(deadAtStart, 32, new Random(42));
        Check(skipActions.All(result => result.Unit.IsAlive), "Dead units cannot act");
        Check(skipActions.Count == 17 && deadAtStart.Round.RoundNumber == 2, "Dead units do not consume action seconds");
    }

    static void CheckMovementAndRange()
    {
        var battle = BattleInitializationSystem.CreateBattle(Config());
        BattleUnit ally = battle.GetUnitAt(9);
        Check(!TryMove(battle, ally), "Enemy blocks forward movement");
        Check(!TryMove(battle, battle.GetUnitAt(3)), "Ally blocks forward movement");
        foreach (BattleUnit unit in battle.Battlefield.Units.Where(unit => unit.Side == BattleSide.Enemy))
            ApplyDamage(battle, unit, 1000);
        Check(!TryMove(battle, ally) && ally.SlotIndex == 9, "No enemies means no target-driven movement");

        battle = BattleInitializationSystem.CreateBattle(Config());
        BattleUnit enemy = battle.GetUnitAt(12);
        Check(!TryMove(battle, enemy), "Enemy cannot cross occupied front cell");
        foreach (BattleUnit unit in battle.Battlefield.Units.Where(unit => unit.Side == BattleSide.Ally))
            ApplyDamage(battle, unit, 1000);
        Check(!TryMove(battle, enemy) && enemy.SlotIndex == 12, "Enemy also stays when no opponent remains");

        battle = BattleInitializationSystem.CreateBattle(Config());
        ally = battle.GetUnitAt(9);
        Check(FindTarget(battle, ally) == battle.GetUnitAt(12), "Adjacent target within one grid distance");
        ApplyDamage(battle, battle.GetUnitAt(12), 1000);
        Check(FindTarget(battle, ally) == null, "Diagonal distance is two, not one");
        battle = BattleInitializationSystem.CreateBattle(Config(2));
        ApplyDamage(battle, battle.GetUnitAt(12), 1000);
        Check(FindTarget(battle, battle.GetUnitAt(9)) == battle.GetUnitAt(13), "Manhattan range includes diagonal at two");
    }

    static void CheckAdjacentAttackPriority()
    {
        foreach (BattleSide side in new[] { BattleSide.Ally, BattleSide.Enemy })
        {
            using var battle = BattleInitializationSystem.CreateBattle(Config(1, defense: 0));
            BattleUnit actor = battle.Battlefield.Units.First(unit => unit.Side == side);
            BattleUnit[] enemies = battle.Battlefield.Units.Where(unit => unit.Side != side).Take(4).ToArray();
            foreach (BattleUnit unit in battle.Battlefield.Units.ToArray())
                if (unit != actor && !enemies.Contains(unit)) ApplyDamage(battle, unit, unit.MaxHp);
            foreach (BattleUnit unit in enemies.Concat(new[] { actor }))
                unit.RemoveComponent<BattlePositionComponent>();
            actor.AddComponent<BattlePositionComponent>().TryPlace(10);
            int[] priority = side == BattleSide.Ally ? new[] { 13, 9, 11, 7 } : new[] { 7, 9, 11, 13 };
            for (int i = 0; i < enemies.Length; i++)
                Check(enemies[i].AddComponent<BattlePositionComponent>().TryPlace(priority[i]), "Place directional enemy");
            for (int i = 0; i < enemies.Length; i++)
            {
                Check(actor.FindAdjacentEnemy() == enemies[i] && FindTarget(battle, actor) == enemies[i],
                    "Both sides prioritize forward, up, down, then back");
                Check(!TryMove(battle, actor) && actor.SlotIndex == 10, "Any adjacent enemy prevents movement");
                ApplyDamage(battle, enemies[i], enemies[i].MaxHp);
            }
            Check(!TryMove(battle, actor) && actor.SlotIndex == 10,
                "After all targets die there is no automatic forward step");
        }

        foreach (int targetSlot in new[] { 9, 11, 7, 12, 14, 16 })
        {
            using var battle = BattleInitializationSystem.CreateBattle(Config(1, defense: 0));
            BattleUnit actor = battle.GetUnitAt(3);
            BattleUnit target = battle.GetUnitAt(18);
            RemoveExcept(battle, 3, 18);
            Check(actor.Position.TryPlace(10) && target.Position.TryPlace(targetSlot), "Arrange movement and attack");
            Tick(battle, 15, new KeepOrderRandom());
            // Duplicate scheduler entries must never grant a second attack in the round.
            battle.Round.ActionOrder.Insert(1, actor);
            var first = Tick(battle, 1, new KeepOrderRandom()).Single();
            int expectedSlot = targetSlot == 12 ? 9 : targetSlot == 14 ? 11 : targetSlot == 16 ? 13 : 10;
            Check(first.FromSlot == 10 && first.ToSlot == expectedSlot && first.Target == target && first.Hits.Count == 1,
                "Attack adjacent enemy in place, otherwise move then attack once");
            Near(target.CurrentHp, 900, "Exactly one attack worth of damage");
            var repeat = new BattleActionResult();
            actor.GetComponent<BattleBasicAttackComponent>().TryAttack(repeat);
            Check(repeat.Hits.Count == 0, "Acted unit cannot attack again");
            Check(Tick(battle, 1, new KeepOrderRandom()).All(action => action.Unit != actor), "Duplicate action entry is skipped");
        }

        using var edge = BattleInitializationSystem.CreateBattle(Config(1));
        BattleUnit edgeActor = edge.GetUnitAt(3);
        BattleUnit edgeTarget = edge.GetUnitAt(18);
        RemoveExcept(edge, 3, 18);
        Check(edgeActor.Position.TryPlace(8) && edgeTarget.Position.TryPlace(9), "Arrange column boundary");
        Check(edgeActor.FindAdjacentEnemy() == null, "Bottom row does not wrap into next column's top row");
        Check(edgeTarget.Position.TryPlace(0) && edgeActor.Position.TryPlace(9) && edgeTarget.Position.TryPlace(8), "Arrange opposite column boundary");
        Check(edgeActor.FindAdjacentEnemy() == null, "Top row does not wrap into previous column's bottom row");
    }

    static void CheckDamageAndActions()
    {
        Near(BattleCombatComponentSystem.HitChance(88, 88), 1, "Equal speed guarantees hit");
        Near(BattleCombatComponentSystem.HitChance(100, 50), 1, "Higher speed capped at guaranteed hit");
        Near(BattleCombatComponentSystem.HitChance(50, 100), 0.5, "Speed difference divided by 100");
        Near(BattleCombatComponentSystem.HitChance(0, 100), 0, "100 speed disadvantage means zero hit chance");
        Near(BattleCombatComponentSystem.HitChance(0, 200), 0, "Negative hit chance clamped");
        Near(BattleCombatComponentSystem.BasicDamage(100, 100), 0, "Equal defense blocks all direct damage");
        Near(BattleCombatComponentSystem.BasicDamage(100, 50), 50, "Defense subtracts directly from attack");
        Near(BattleCombatComponentSystem.BasicDamage(100, 150), 0, "Higher defense never produces negative damage");
        Near(BattleCombatComponentSystem.BasicDamage(100, 0), 100, "Zero defense takes full attack damage");

        var battle = BattleInitializationSystem.CreateBattle(Config(1, defense: 50));
        RemoveExcept(battle, 6, 12);
        BattleRoundComponent turns = battle.Round;
        var random = new KeepOrderRandom();
        var actions = new List<BattleActionResult>();
        actions.AddRange(Tick(battle, 16, random));
        BattleActionResult first = actions.Single();
        Check(first.FromSlot == 6 && first.ToSlot == 9 && first.Target.SlotIndex == 12, "Move before range check and attack");
        Check(first.Hit, "Equal-speed attack always hits");
        Near(first.Damage, 50, "Basic damage applied once after subtracting defense");
        Near(first.Target.CurrentHp, 950, "HP loses attack minus defense");
        actions.AddRange(Tick(battle, 1, random));
        Check(actions.Count == 2 && turns.RoundNumber == 2, "Each of two surviving units gets one action");

        battle = BattleInitializationSystem.CreateBattle(Config(1, hp: 50, defense: 0));
        RemoveExcept(battle, 9, 12);
        var doomed = battle.GetUnitAt(12);
        turns = battle.Round;
        int attacks = Tick(battle, 100, new KeepOrderRandom()).Count;
        Check(turns.Phase == BattlePhase.Finished && turns.Winner == BattleSide.Ally && attacks == 1, "Finish immediately after final enemy dies");
        Check(!doomed.IsAlive && doomed.CurrentHp == 0 && battle.GetUnitAt(12) == null, "Death frees slot and HP never goes negative");
        Check(battle.GetUnitAt(9).CurrentHp == 50, "Defeated unit never retaliates");
        Check(!TryMove(battle, doomed), "Dead unit cannot move");
    }

    static void CheckActualConfig()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        const string sample = "人物名称,血狼人";
        Check(ExcelDataMgr.DecodeTableText(Encoding.UTF8.GetBytes(sample)) == sample, "UTF-8 names decode correctly");
        Check(ExcelDataMgr.DecodeTableText(Encoding.UTF8.GetBytes("\uFEFF" + sample)) == sample, "UTF-8 BOM removed");
        Check(ExcelDataMgr.DecodeTableText(Encoding.GetEncoding(936).GetBytes(sample)) == sample, "GBK names decode correctly");
        ExcelDataMgr.Instance.Init();
        character config = ExcelDataMgr.Instance.GetConfigData<character>("1001");
        Check(config.name == "血狼人" && config.attackRange == 1, "Actual table has a readable name and one-cell melee range");
        var battle = BattleInitializationSystem.CreateBattle(config);
        for (int row = 0; row < BattleEntity.RowCount; row++)
        {
            BattleUnit ally = battle.GetUnitAt(9 + row);
            BattleUnit enemy = battle.GetUnitAt(12 + row);
            Check(FindTarget(battle, ally) == enemy && FindTarget(battle, enemy) == ally,
                "Both front lines can attack their adjacent opponent");
        }
    }

    static void CheckComponentIsolation()
    {
        character config = Config();
        BattleEntity battle = BattleInitializationSystem.CreateBattle(config);
        var units = battle.Battlefield.Units;
        Check(units.Count == 18 && battle.Battlefield.SlotCount == 24, "Initial battle entity layout");
        Check(units.Select(u => u.Identity).Distinct().Count() == 18, "Independent identity components");
        Check(units.Select(u => u.Position).Distinct().Count() == 18, "Independent position components");
        Check(units.Select(u => u.Health).Distinct().Count() == 18, "Independent health components");
        Check(units.Select(u => u.Combat).Distinct().Count() == 18, "Independent combat components");
        Check(units.Select(u => u.Skills).Distinct().Count() == 18, "Independent skill components");
        Check(units.Select(u => u.Action).Distinct().Count() == 18, "Independent action components");
        for (int row = 0; row < 3; row++)
            Check(battle.GetUnitAt(row) == null && battle.GetUnitAt(21 + row) == null, "First and eighth columns start empty");
        for (int i = 3; i < 21; i++)
        {
            BattleUnit unit = battle.GetUnitAt(i);
            Check(unit.Side == (i < 12 ? BattleSide.Ally : BattleSide.Enemy), "Center allies/right enemies");
            Check(unit.Skill1Id == config.skill1Id && unit.Skill2Id == config.skill2Id && unit.Skill3Id == config.skill3Id, "Skill data copied from config");
        }

        BattleEntity other = BattleInitializationSystem.CreateBattle(config);
        BattleUnit first = battle.GetUnitAt(3);
        BattleUnit second = battle.GetUnitAt(4);
        ApplyDamage(battle, first, 25);
        Check(first.CurrentHp == first.Health.CurrentHp && first.CurrentHp == config.hp - 25, "Queries read live component values");
        Check(second.CurrentHp == config.hp && other.GetUnitAt(3).CurrentHp == config.hp, "Damage stays in this unit and battle");
        Check(config.hp == 1000 && config.attack == 100 && config.skill1Id == 100101, "Runtime does not write to config");
        config.attack = 999;
        config.skill1Id = 999;
        Check(first.Attack == 100 && first.Skill1Id == 100101, "Entities retain independent config snapshots");

        Tick(battle, 16, new Random(42));
        Check(battle.Round.Phase == BattlePhase.Combat && other.Round.Phase == BattlePhase.Preparation,
            "Round data belongs to its battle entity");
        Check(other.Round.PreparationRemaining == 15 && other.Round.ActionOrder.Count == 0, "Action order and timer are not shared across battles");
    }

    static void CheckWorldIsolation()
    {
        BattleEntity first = BattleInitializationSystem.CreateBattle(Config());
        BattleEntity unrelated = BattleInitializationSystem.CreateBattle(Config(0));
        BattleEntity control = BattleInitializationSystem.CreateBattle(Config());
        var firstRandom = new Random(17);
        var unrelatedRandom = new Random(73);
        var controlRandom = new Random(17);
        var firstActions = new List<BattleActionResult>();
        var controlActions = new List<BattleActionResult>();
        for (int second = 0; second < 66; second++)
        {
            firstActions.AddRange(Tick(first, 1, firstRandom));
            Tick(unrelated, 0.5, unrelatedRandom);
            Tick(unrelated, 0.5, unrelatedRandom);
            controlActions.AddRange(Tick(control, 1, controlRandom));
        }
        Func<BattleActionResult, string> trace = a => a.RoundNumber + "/" + a.Unit.UnitId + "/" +
            a.Target?.UnitId + "/" + a.Hit + "/" + a.Damage;
        Check(firstActions.Select(trace).SequenceEqual(controlActions.Select(trace)), "Interleaved battles keep independent randomness and action order");
        Check(first.Battlefield.Units.Select(u => u.CurrentHp).SequenceEqual(control.Battlefield.Units.Select(u => u.CurrentHp)), "World updates do not leak damage across battles");
        Check(first.Round.RoundNumber == control.Round.RoundNumber && first.Round.PreparationRemaining == control.Round.PreparationRemaining,
            "Lifecycle updates keep each battle's own clock");

        BattleEntity batch = BattleInitializationSystem.CreateBattle(Config(2));
        RemoveExcept(batch, 6, 15);
        batch.GetUnitAt(15).Combat.AttackRange = 1;
        IReadOnlyList<BattleActionResult> results = Tick(batch, 17, new KeepOrderRandom());
        Check(results.Count == 2 && results[0].TargetSlot == 15 && results[0].Target.SlotIndex == 12,
            "Returned actions retain impact positions when their target moves later in the same update");
    }

    static void CheckDynamicCapabilities()
    {
        BattleEntity waiting = BattleInitializationSystem.CreateBattle(Config(2));
        RemoveExcept(waiting, 6, 15);
        Check(Tick(waiting, 15, new KeepOrderRandom()).Count == 0 && waiting.GetUnitAt(6) != null && waiting.GetUnitAt(15).CurrentHp == 1000,
            "Attaching movement and attack provides capabilities without acting during preparation");
        BattleEntity battle = BattleInitializationSystem.CreateBattle(Config(2));
        RemoveExcept(battle, 9, 15);
        BattleUnit unit = battle.GetUnitAt(9);
        unit.RemoveComponent<BattleMovementComponent>();
        BattleActionResult result = Tick(battle, 16, new KeepOrderRandom()).Single();
        Check(result.FromSlot == 9 && result.ToSlot == 9 && result.Hit, "No movement component: stays put and can still attack");

        battle = BattleInitializationSystem.CreateBattle(Config(2));
        RemoveExcept(battle, 6, 15);
        unit = battle.GetUnitAt(6);
        unit.RemoveComponent<BattleBasicAttackComponent>();
        result = Tick(battle, 16, new KeepOrderRandom()).Single();
        Check(result.ToSlot == 9 && result.Target == null && battle.GetUnitAt(15).CurrentHp == 1000,
            "No basic attack component: moves but does not attack");
        unit.AddComponent<BattleBasicAttackComponent>();
        var extra = new BattleActionResult();
        unit.GetComponent<BattleBasicAttackComponent>().TryAttack(extra);
        Check(extra.Target == null, "Reattaching attack cannot grant another action in the same round");
        Tick(battle, 1, new KeepOrderRandom());
        Check(Tick(battle, 16, new KeepOrderRandom()).Single().Hit, "Attack restored for next round");

        battle = BattleInitializationSystem.CreateBattle(Config(2));
        RemoveExcept(battle, 6, 15);
        unit = battle.GetUnitAt(6);
        unit.RemoveComponent<BattleMovementComponent>();
        unit.RemoveComponent<BattleBasicAttackComponent>();
        result = Tick(battle, 16, new KeepOrderRandom()).Single();
        Check(result.FromSlot == result.ToSlot && result.Target == null, "No capabilities: no movement or attack");

        battle = BattleInitializationSystem.CreateBattle(Config(2));
        RemoveExcept(battle, 6, 15);
        unit = battle.GetUnitAt(6);
        unit.RemoveComponent<BattleMovementComponent>();
        unit.AddComponent<BattleMovementComponent>();
        Check(Tick(battle, 16, new KeepOrderRandom()).Single().ToSlot == 9, "Movement restored before pending action");

        battle = BattleInitializationSystem.CreateBattle(Config());
        unit = battle.GetUnitAt(9);
        BattlePositionComponent oldPosition = unit.Position;
        unit.RemoveComponent<BattlePositionComponent>();
        Check(oldPosition.IsDisposed && battle.GetUnitAt(9) == null && unit.SlotIndex == -1, "Position destruction releases occupancy");
        Check(TryMove(battle, battle.GetUnitAt(6)), "Another unit can occupy the released slot");
        BattlePositionComponent replacement = unit.AddComponent<BattlePositionComponent>();
        Check(!replacement.TryPlace(9) && !replacement.TryPlace(0) && replacement.TryPlace(21), "Position validates occupancy and forbids ally entry to column one");

        battle = BattleInitializationSystem.CreateBattle(Config());
        RemoveExcept(battle, 9, 12);
        unit = battle.GetUnitAt(9);
        BattleUnit target = battle.GetUnitAt(12);
        target.RemoveComponent<BattleHealthComponent>();
        Check(FindTarget(battle, unit) == null && target.Health.ApplyDamage(50) == 0, "Missing health is not an attack target and takes no damage");
        BattleHealthComponent health = target.AddComponent<BattleHealthComponent>();
        health.MaxHp = 1000;
        health.CurrentHp = 1000;
        target.RemoveComponent<BattleCombatComponent>();
        Check(FindTarget(battle, unit) == null, "Missing combat attributes skips target");
        target.AddComponent<BattleCombatComponent>();
        Check(FindTarget(battle, unit) == target, "Restoring target data restores targeting");

        battle = BattleInitializationSystem.CreateBattle(Config());
        RemoveExcept(battle, 9, 12);
        unit = battle.GetUnitAt(9);
        unit.RemoveComponent<BattleActionComponent>();
        Tick(battle, 15, new KeepOrderRandom());
        unit.AddComponent<BattleActionComponent>();
        IReadOnlyList<BattleActionResult> actions = Tick(battle, 1, new KeepOrderRandom());
        Check(actions.Count == 1 && actions[0].Unit != unit && battle.Round.RoundNumber == 2, "New eligibility enters the next round's order");
        Check(Tick(battle, 16, new KeepOrderRandom()).Single().Unit == unit, "Newly eligible unit acts next round");

        battle = BattleInitializationSystem.CreateBattle(Config());
        battle.Enabled = false;
        Tick(battle, 16, new KeepOrderRandom());
        Check(battle.Round.PreparationRemaining == 15, "Disabled battle pauses its component lifecycle");
        battle.Enabled = true;
        Tick(battle, 15, new KeepOrderRandom());
        Check(battle.Round.Phase == BattlePhase.Combat, "Enabled battle resumes lifecycle");
        BattleRoundComponent oldRound = battle.Round;
        battle.RemoveComponent<BattleRoundComponent>();
        battle.World.Update(100);
        Check(oldRound.IsDisposed && battle.Round == null && oldRound.ActionOrder.Count == 0, "Removing round stops updates and clears action data");
        battle.AddComponent<BattleRoundComponent>();
        battle.World.Update(14);
        Check(battle.Round.PreparationRemaining == 1, "Reattached round initializes and registers automatically");
        BattleUnit child = battle.Battlefield.Units[0];
        BattleRoundComponent round = battle.Round;
        battle.Dispose();
        Check(child.IsDisposed && round.IsDisposed && battle.Parent == null, "Battle disposal destroys its full entity tree");
        battle.World.Update(100);
    }

    static void Main()
    {
        DamageStatisticsChecks.Run();
        MovementChecks.Run();
        CheckClockAndOrder();
        CheckMovementAndRange();
        CheckAdjacentAttackPriority();
        CheckDamageAndActions();
        CheckActualConfig();
        CheckComponentIsolation();
        CheckWorldIsolation();
        CheckDynamicCapabilities();
        LifecycleChecks.Run();
        SkillChecks.Run();
        ThunderChecks.Run();
        EnemyAIChecks.Run();
        MoonChecks.Run();
        CooldownChecks.Run();
        WolfComboChecks.Run();
        JudicatorChecks.Run();
        Console.WriteLine("PASS: preparation/action timing, repeated rounds, randomized order, movement/blocking/bounds, Manhattan range, hit formula, subtractive defense damage, death and victory.");
    }
}
