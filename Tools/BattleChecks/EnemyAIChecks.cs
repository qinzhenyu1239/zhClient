using System;
using System.Linq;

static class EnemyAIChecks
{
    sealed class KeepOrderRandom : Random
    {
        public override int Next(int maxValue) => maxValue - 1;
    }
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Enemy AI: " + message);
    }
    static BattleEntity Create()
    {
        var config = new character { ID = 1001, name = "血狼人", hp = 1000, attack = 100, defense = 20,
            speed = 88, attackRange = 1, skill2Id = BattleSkillDefinition.Lifesteal };
        var skill = new skill { ID = BattleSkillDefinition.Lifesteal, param1 = "10", param2 = "5", skillRange = 3, skillPointCost = 1 };
        return BattleInitializationSystem.CreateBattle(config, random: new KeepOrderRandom(), skillConfigs: new[] { skill });
    }
    static void ClearEnemyPlans(BattleEntity battle)
    {
        foreach (var enemy in battle.Battlefield.Units.Where(unit => unit.Side == BattleSide.Enemy)) enemy.Skills.CancelPendingSkill();
    }
    static void CheckBudgetsAndRefunds()
    {
        using var battle = Create();
        var pool = battle.GetComponent<BattleSkillPointComponent>();
        var enemy = battle.GetUnitAt(12);
        var ally = battle.GetUnitAt(9);
        Check(pool.PointsPerRound == 30 && pool.Current == 30 && pool.EnemyCurrent == 21, "separate 30-point budgets and nine AI reservations");
        Check(battle.Battlefield.Units.Where(unit => unit.Side == BattleSide.Enemy).All(unit => unit.Skills.PendingSkillId == BattleSkillDefinition.Lifesteal),
            "AI preselects a usable skill for every enemy");
        Check(ally.Skills.TryQueue(BattleSkillDefinition.Lifesteal, out _) && pool.Current == 29 && pool.EnemyCurrent == 21,
            "player selection only spends ally points");
        enemy.Skills.CancelPendingSkill();
        Check(pool.Current == 29 && pool.EnemyCurrent == 22, "enemy cancellation refunds only enemy budget");
        Check(enemy.Skills.TryQueue(BattleSkillDefinition.Lifesteal, out _), "enemy uses same skill queue API");
        Check(pool.Current == 29 && pool.EnemyCurrent == 21, "enemy queue never spends ally points");
        enemy.Health.ApplyDamage(enemy.MaxHp);
        Check(pool.Current == 29 && pool.EnemyCurrent == 22, "enemy death refunds its own reservation");
        ClearEnemyPlans(battle);
        Check(pool.EnemyCurrent == 30 && pool.TrySpend(30, BattleSide.Enemy), "enemy may spend exactly its own full budget");
        battle.GetComponent<BattleEnemyAIComponent>().PlanSkills();
        Check(pool.EnemyCurrent == 0 && pool.Current == 29 && !enemy.Skills.CanQueue(BattleSkillDefinition.Lifesteal, out _),
            "AI cannot borrow player points when empty");
        pool.BeginRound(2);
        Check(pool.Current == 30 && pool.EnemyCurrent == 30, "both budgets reset together");
        ally.Skills.CancelPendingSkill();
        Check(pool.Current == 30 && pool.EnemyCurrent == 30, "old-round reservations cannot inflate either new budget");
    }
    static void CheckAIRestrictions()
    {
        using var battle = Create();
        ClearEnemyPlans(battle);
        var ai = battle.GetComponent<BattleEnemyAIComponent>();
        var enemy = battle.GetUnitAt(12);
        var silence = enemy.AddComponent<BattleSilenceComponent>();
        silence.FirstRound = silence.LastRound = 1;
        ai.TrySelectSkill(enemy);
        Check(enemy.Skills.PendingSkillId == 0, "silenced enemy cannot select skills");
        silence.Dispose();
        enemy.Action.HasActedThisRound = true;
        ai.TrySelectSkill(enemy);
        Check(enemy.Skills.PendingSkillId == 0, "already acted enemy cannot reserve an extra action");
        enemy.Action.HasActedThisRound = false;
        ai.TrySelectSkill(enemy);
        Check(enemy.Skills.PendingSkillId != 0, "eligible enemy actively selects");
        int points = battle.GetComponent<BattleSkillPointComponent>().EnemyCurrent;
        ai.TrySelectSkill(enemy);
        Check(battle.GetComponent<BattleSkillPointComponent>().EnemyCurrent == points, "repeated planning does not charge twice");
        enemy.Skills.CancelPendingSkill();
        var steal = enemy.AddComponent<BattleLifestealComponent>();
        steal.Stacks = steal.MaxStacks = 5;
        ai.TrySelectSkill(enemy);
        Check(enemy.Skills.PendingSkillId == 0, "capped buff is not selected");
        ai.TrySelectSkill(battle.GetUnitAt(9));
        Check(battle.GetUnitAt(9).Skills.PendingSkillId == 0, "AI never chooses for the player");
    }
    static void CheckScheduledActions()
    {
        using var battle = Create();
        var enemy = battle.GetUnitAt(12);
        var ally = battle.GetUnitAt(9);
        foreach (var unit in battle.Battlefield.Units)
            if (unit != enemy && unit != ally) unit.RemoveComponent<BattleActionComponent>();
        battle.World.Update(17);
        var results = battle.Round.DrainActions();
        Check(results.Count == 2 && results.Count(action => action.Unit == enemy && action.Kind == BattleActionKind.Skill) == 1,
            "enemy executes its queued skill once instead of a basic attack");
        Check(enemy.GetComponent<BattleLifestealComponent>()?.Stacks == 1 && battle.Round.RoundNumber == 2,
            "enemy skill effect and normal round lifecycle");
        var pool = battle.GetComponent<BattleSkillPointComponent>();
        Check(pool.Current == 30 && pool.EnemyCurrent == 29 && enemy.Skills.PendingSkillId != 0,
            "next preparation resets both pools then AI reserves only for eligible enemies");
        using var fallback = Create();
        ClearEnemyPlans(fallback);
        var fallbackEnemy = fallback.GetUnitAt(12);
        var fallbackAlly = fallback.GetUnitAt(9);
        foreach (var unit in fallback.Battlefield.Units)
            if (unit != fallbackEnemy && unit != fallbackAlly) unit.RemoveComponent<BattleActionComponent>();
        fallback.GetComponent<BattleSkillPointComponent>().TrySpend(30, BattleSide.Enemy);
        fallback.World.Update(17);
        Check(fallback.Round.DrainActions().Any(action => action.Unit == fallbackEnemy && action.Kind == BattleActionKind.BasicAttack && action.Hits.Count == 1),
            "empty budget falls back to basic attack");
    }
    static void CheckEnemyThundercloud()
    {
        var config = new character { ID = 1002, name = "雷神", hp = 1400, attack = 130, defense = 30,
            speed = 75, attackRange = 2, skill1Id = BattleSkillDefinition.ThunderStrike, skill3Id = BattleSkillDefinition.Thundercloud };
        var records = new[] {
            new skill { ID = BattleSkillDefinition.ThunderStrike, param1 = "150", param2 = "1", skillRange = 99, skillPointCost = 2 },
            new skill { ID = BattleSkillDefinition.Thundercloud, param1 = "4", param2 = "200", param3 = "400", param4 = "2", skillRange = 99, skillPointCost = 5 }
        };
        using var battle = BattleInitializationSystem.CreateBattle(config, random: new KeepOrderRandom(), skillConfigs: records);
        var pool = battle.GetComponent<BattleSkillPointComponent>();
        Check(pool.EnemyCurrent == 0 && pool.Current == 30 &&
            battle.Battlefield.Units.Count(unit => unit.Side == BattleSide.Enemy && unit.Skills.PendingSkillId != 0) == 6,
            "AI respects 30-point cap with five-point skills");
        var enemy = battle.GetUnitAt(12);
        foreach (var unit in battle.Battlefield.Units)
            if (unit != enemy && unit.SlotIndex != 9) unit.RemoveComponent<BattleActionComponent>();
        battle.World.Update(21);
        var actions = battle.Round.DrainActions();
        Check(actions.Any(action => action.Unit == enemy && action.Kind == BattleActionKind.Skill && action.SkillId == BattleSkillDefinition.Thundercloud) &&
            actions.Count(action => action.Unit == enemy && action.Kind == BattleActionKind.Lightning) == 4,
            "enemy Thor casts its cloud and produces all four timed strikes");
        Check(pool.Current == 30 && pool.EnemyCurrent == 28, "active cloud is not repurchased; AI chooses two-point strike next round");
    }
    public static void Run()
    {
        CheckBudgetsAndRefunds();
        CheckAIRestrictions();
        CheckScheduledActions();
        CheckEnemyThundercloud();
        Console.WriteLine("PASS: enemy AI preparation/action skill selection, separate equal budgets, side-specific spending/refunds, silence/cap/action restrictions, next-round planning and basic fallback.");
    }
}
