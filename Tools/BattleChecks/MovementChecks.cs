using System;
using System.Linq;
using System.Collections.Generic;

static class MovementChecks
{
    sealed class FixedRandom : Random { public override int Next(int maxValue) => maxValue - 1; }
    static void Check(bool value, string message) { if (!value) throw new Exception("Movement: " + message); }
    static BattleEntity Create(int actorSlot = 9, int targetSlot = 18, float range = 1, bool assassin = false, bool skill = false)
    {
        var actor = new character { ID = 1001, hp = 1000, attack = 100, defense = 0, speed = 100, attackRange = range,
            battleType = assassin ? 3 : 1, skill1Id = skill ? 100401 : 0, skill2Id = skill ? 100102 : 0 };
        var enemy = new character { ID = 1002, hp = 1000, attack = 100, speed = 100, attackRange = 1, battleType = 1 };
        var records = new[] {
            new skill { ID = 100401, param1 = "150", param2 = "15", skillRange = 5, skillPointCost = 5 },
            new skill { ID = 100102, param1 = "20", param2 = "5", skillRange = 99, skillPointCost = 5 }
        };
        var battle = BattleInitializationSystem.CreateBattle(enemy, random: new FixedRandom(), skillConfigs: records,
            lineup: new Dictionary<int, character> { { actorSlot, actor }, { targetSlot, enemy } }, fillEmptySlots: false);
        battle.GetComponent<BattleEnemyAIComponent>().Enabled = false;
        foreach (var unit in battle.Battlefield.Units) unit.Skills.CancelPendingSkill();
        return battle;
    }
    static bool Move(BattleUnit unit) => unit.GetComponent<BattleMovementComponent>().TryMoveTowardEnemy();
    static int Distance(BattleUnit a, BattleUnit b) => BattleMovementComponentSystem.GridDistance(a.SlotIndex, b.SlotIndex, 3);
    static void CheckPursuit()
    {
        foreach (var pair in new[] { new[] { 9, 18 }, new[] { 18, 9 } })
        {
            using var battle = Create(pair[0], pair[1]);
            var actor = battle.GetUnitAt(pair[0]); var target = battle.GetUnitAt(pair[1]);
            for (int step = 0; step < 2; step++)
            {
                int from = actor.SlotIndex, before = Distance(actor, target);
                Check(Move(actor) && Distance(actor, target) == before - 1 &&
                    BattleMovementComponentSystem.GridDistance(from, actor.SlotIndex, 3) == 1, "both sides move exactly one adjacent cell toward target");
                Check(battle.GetUnitAt(from) == null && battle.GetUnitAt(actor.SlotIndex) == actor, "occupancy updates atomically");
            }
            Check(!Move(actor) && Distance(actor, target) == 1, "stop on entering basic range");
        }
        foreach (int destination in new[] { 4, 11 })
        {
            using var battle = Create(9, 18);
            var actor = battle.GetUnitAt(9); var target = battle.GetUnitAt(18);
            Check(actor.Position.TryPlace(destination == 11 ? 9 : 10) && target.Position.TryPlace(destination), "arrange rear or vertical target");
            int distance = Distance(actor, target);
            Check(Move(actor) && Distance(actor, target) == distance - 1, "can move backward or vertically instead of always forward");
        }
        using var none = Create();
        var alone = none.GetUnitAt(9);
        none.GetUnitAt(18).Health.Execute();
        Check(!Move(alone) && alone.SlotIndex == 9, "no enemies means no movement");
    }
    static void CheckRanges()
    {
        using var ranged = Create(9, 15, range: 2);
        Check(!Move(ranged.GetUnitAt(9)), "ranged unit stops before enemies become adjacent");
        using var skillBattle = Create(skill: true);
        var actor = skillBattle.GetUnitAt(9);
        Check(Move(actor), "unselected long-range skill does not freeze the unit");
        Check(actor.Position.TryPlace(9) && actor.Skills.TryQueue(100401, out _), "select ranged damage skill");
        Check(!Move(actor), "selected usable ranged attack stops movement");
        skillBattle.Round.Phase = BattlePhase.Combat;
        var result = new BattleActionResult();
        Check(actor.Skills.TryExecutePending(result) && result.Hits.Count == 1 && actor.SlotIndex == 9,
            "selected skill fires from current range without unwanted forward step");
        actor.Skills.CooldownReadyRounds[100401] = 99;
        Check(Move(actor), "unavailable skill does not extend movement stop range");
        using var buff = Create(skill: true);
        var buffer = buff.GetUnitAt(9);
        Check(buffer.Skills.TryQueue(100102, out _) && Move(buffer), "global self buff keeps normal basic pursuit range");
        using var arrival = Create(6, 15, range: 2);
        arrival.World.Update(16);
        var action = arrival.Round.DrainActions().Single();
        Check(action.FromSlot == 6 && action.ToSlot == 9 && action.Hits.Count == 1,
            "move one cell into range then attack in the same action");
    }
    static void CheckObstacles()
    {
        var config = new character { ID = 1001, hp = 1000, attackRange = 1, battleType = 1 };
        using var nearest = BattleInitializationSystem.CreateBattle(config, lineup: new Dictionary<int, character> {
            { 10, config }, { 12, config }, { 18, config } }, fillEmptySlots: false);
        var pursuer = nearest.GetUnitAt(10);
        var close = nearest.GetUnitAt(12); var farther = nearest.GetUnitAt(18);
        Check(close.Position.TryPlace(4) && Move(pursuer) && pursuer.SlotIndex == 7,
            "near enemy behind outranks a more distant enemy ahead");
        close.Health.Execute();
        int beforeRetarget = Distance(pursuer, farther);
        Check(Move(pursuer) && Distance(pursuer, farther) == beforeRetarget - 1,
            "dead nearest enemy is replaced by the next living target");
        using var battle = BattleInitializationSystem.CreateBattle(config, lineup: new Dictionary<int, character> {
            { 3, config }, { 6, config }, { 12, config } }, fillEmptySlots: false);
        var actor = battle.GetUnitAt(3);
        Check(Move(actor) && actor.SlotIndex == 4 && battle.GetUnitAt(6) != null,
            "find a one-cell detour without walking through an occupied cell");
        using var boxed = BattleInitializationSystem.CreateBattle(config, lineup: new Dictionary<int, character> {
            { 3, config }, { 4, config }, { 5, config }, { 6, config }, { 10, config }, { 18, config } }, fillEmptySlots: false);
        foreach (var pair in new[] { new[] { 3, 7 }, new[] { 4, 9 }, new[] { 5, 11 }, new[] { 6, 13 } })
            Check(boxed.GetUnitAt(pair[0]).Position.TryPlace(pair[1]), "place surrounding blockers");
        Check(!Move(boxed.GetUnitAt(10)), "fully surrounded unit waits without overlap");
        using var edge = Create(6, 12);
        var bottom = edge.GetUnitAt(6); var target = edge.GetUnitAt(12);
        Check(bottom.Position.TryPlace(8) && target.Position.TryPlace(9), "arrange row boundary");
        int from = bottom.SlotIndex;
        Check(Move(bottom) && bottom.SlotIndex != 9 && BattleMovementComponentSystem.GridDistance(from, bottom.SlotIndex, 3) == 1,
            "vertical neighbors cannot wrap around a column boundary");
    }
    static void CheckOpeningAssassins()
    {
        foreach (var pair in new[] { new[] { 3, 18, 21 }, new[] { 18, 3, 0 } })
        {
            using var battle = Create(pair[0], pair[1], range: 99, assassin: true);
            var actor = battle.GetUnitAt(pair[0]);
            var assassin = actor.GetComponent<BattleAssassinComponent>();
            Check(assassin.OpeningInfiltrationEligible && Move(actor) && actor.SlotIndex == pair[2] && assassin.HasInfiltrated,
                "only opening home-rear placement grants mirrored jump, before ordinary range stopping");
            Check(!assassin.TryInfiltrate(), "opening infiltration cannot repeat");
            Check(!Move(actor), "after opening jump ordinary in-range rule applies");
        }
        foreach (var pair in new[] { new[] { 6, 18, 3 }, new[] { 12, 3, 18 } })
        {
            using var battle = Create(pair[0], pair[1], assassin: true);
            var actor = battle.GetUnitAt(pair[0]); var enemy = battle.GetUnitAt(pair[1]);
            var assassin = actor.GetComponent<BattleAssassinComponent>();
            Check(!assassin.OpeningInfiltrationEligible, "other opening columns do not grant infiltration");
            int before = Distance(actor, enemy);
            Check(Move(actor) && Distance(actor, enemy) == before - 1 && !assassin.HasInfiltrated,
                "noneligible assassin pursues nearest enemy rather than retreating");
            Check(actor.Position.TryPlace(pair[2]) && !assassin.TryInfiltrate(), "moving to home rear later never grants new eligibility");
        }
        var normal = new character { ID = 1001, hp = 1000, attackRange = 1, battleType = 1 };
        var blade = new character { ID = 1003, hp = 1000, attackRange = 1, battleType = 3 };
        using var blocked = BattleInitializationSystem.CreateBattle(normal, lineup: new Dictionary<int, character> {
            { 3, blade }, { 6, normal }, { 7, normal }, { 8, normal }, { 18, normal } }, fillEmptySlots: false);
        var opener = blocked.GetUnitAt(3);
        for (int row = 0; row < 3; row++)
            Check(blocked.GetUnitAt(6 + row).Position.TryPlace(21 + row), "occupy every enemy-rear landing cell");
        Check(Move(opener) && opener.SlotIndex == 6 && !opener.GetComponent<BattleAssassinComponent>().HasInfiltrated,
            "blocked opening jump falls back to normal pursuit without overwriting occupants");
        blocked.GetUnitAt(21).Health.Execute();
        Check(Move(opener) && opener.SlotIndex == 21, "initially eligible assassin can retry when a landing cell opens");
    }
    public static void Run()
    {
        CheckPursuit(); CheckRanges(); CheckObstacles(); CheckOpeningAssassins();
        Console.WriteLine("PASS: target-driven four-way one-cell pursuit, range stopping, selected attack vs self-buff range, same-action attack, obstacles/row boundaries, opening-only mirrored assassin infiltration, no retreat or late eligibility.");
    }
}
