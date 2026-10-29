using System;
using System.Linq;
using System.Collections.Generic;

static class JudicatorChecks
{
    const int Caster = 9, Target = 12, Friend = 6;
    sealed class FixedRandom : Random
    {
        public override int Next(int maxValue) => maxValue - 1;
        public override double NextDouble() => 0;
    }
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception("Judicator: " + message);
    }
    static void Near(float actual, float expected, string message) => Check(Math.Abs(actual - expected) < .001f, message + " actual=" + actual);
    static skill[] Definitions() => new[]
    {
        new skill { ID = 100401, param1 = "150", param2 = "15", skillRange = 99, skillPointCost = 5 },
        new skill { ID = 100402, param1 = "100", param2 = "10", skillRange = 99, skillPointCost = 5 },
        new skill { ID = 100403, param1 = "15", param2 = "2", skillRange = 99, skillPointCost = 10 }
    };
    static BattleEntity Create(bool enemyCaster = false, bool secondCaster = false)
    {
        var wolf = new character { ID = 1001, name = "fixture", hp = 1000, attack = 100, defense = 20, speed = 80, attackRange = 1, battleType = 1 };
        var judge = new character { ID = 1004, name = "裁决使", hp = 1200, attack = 110, defense = 25, speed = 80, attackRange = 2, battleType = 2,
            skill1Id = 100401, skill2Id = 100402, skill3Id = 100403 };
        var lineup = new Dictionary<int, character> { { Friend, wolf }, { Caster, enemyCaster ? wolf : judge },
            { Target, enemyCaster ? judge : wolf }, { 13, wolf }, { 15, wolf } };
        if (secondCaster) lineup.Add(10, judge);
        return BattleInitializationSystem.CreateBattle(wolf, random: new FixedRandom(), skillConfigs: Definitions(), lineup: lineup, fillEmptySlots: false);
    }
    static BattleActionResult Cast(BattleUnit unit, int id)
    {
        var battle = unit.GetParent<BattleEntity>();
        battle.Round.Phase = BattlePhase.Combat;
        Check(unit.Skills.TryQueue(id, out string reason), "queue " + id + ": " + reason);
        var result = new BattleActionResult { Unit = unit, RoundNumber = battle.Round.RoundNumber, FromSlot = unit.SlotIndex, ToSlot = unit.SlotIndex };
        Check(unit.Skills.TryExecutePending(result), "execute " + id);
        return result;
    }
    static void CheckLiveConfig()
    {
        var hero = ExcelDataMgr.Instance.GetConfigData<character>("1004");
        Check(hero.name == "裁决使" && hero.hp > 0 && hero.attack > 0 && hero.skill1Id == 100401 && hero.skill2Id == 100402 && hero.skill3Id == 100403,
            "live character record and skill IDs");
        var all = ExcelDataMgr.Instance.GetConfigDatas<skill>(null);
        var beam = new BattleSkillDefinition(all.Single(s => s.ID == 100401));
        var ward = new BattleSkillDefinition(all.Single(s => s.ID == 100402));
        var aura = new BattleSkillDefinition(all.Single(s => s.ID == 100403));
        Check(beam.Value(1) == 150 && beam.Value(2) > 0 && beam.Value(2) <= 100 && ward.Value(1) == 100 && ward.Int(2) == 10 && aura.Value(1) > 0 && aura.Value(1) <= 100 && aura.Int(2) == 2,
            "live skill parameters support editable execution thresholds; fixed execution scenarios use fixtures");
        Check(beam.PointCost == 5 && ward.PointCost == 5 && aura.PointCost == 10 && beam.CooldownRounds == 3 && ward.CooldownRounds == 3 && aura.CooldownRounds == 5,
            "configured costs and cooldowns");
        Check(!aura.FormatDescription().Contains("param"), "descriptions substitute parameters");
        var heroes = ExcelDataMgr.Instance.GetConfigDatas<character>(null);
        using var battle = BattleInitializationSystem.CreateRandomBattle(heroes, random: new FixedRandom(), skillConfigs: all);
        foreach (var side in new[] { BattleSide.Ally, BattleSide.Enemy })
            Check(battle.Battlefield.Units.Count(u => u.Side == side && u.CharacterId == 1004) == 1, "random roster includes one judicator per side");
    }
    static void CheckBeam()
    {
        foreach (float initialHp in new[] { 300f, 295f, 294f })
        {
            using var battle = Create();
            var target = battle.GetUnitAt(Target);
            target.Health.CurrentHp = initialHp;
            var result = Cast(battle.GetUnitAt(Caster), 100401);
            if (initialHp >= 295)
            {
                Check(target.IsAlive && !result.Hits[0].Executed, "15% equality and higher survive");
                Near(target.CurrentHp, initialHp - 145, "150 percent attack minus defense");
            }
            else
            {
                Check(!target.IsAlive && result.Hits.Single().Executed && battle.GetUnitAt(Target) == null, "under-15% post-hit execution frees occupancy");
                Near(result.Damage, initialHp, "execution records all actual HP removed");
            }
        }
        using var shielded = Create();
        var protectedTarget = shielded.GetUnitAt(Target);
        protectedTarget.Health.CurrentHp = 100;
        protectedTarget.AddPermanentShield(1000, 0);
        var blocked = Cast(shielded.GetUnitAt(Caster), 100401);
        Check(protectedTarget.IsAlive && blocked.Damage == 0 && !blocked.Hits[0].Executed, "beam requires actual HP damage to execute");
        using var missed = Create();
        missed.GetUnitAt(Caster).Combat.Speed = 0;
        missed.GetUnitAt(Target).Combat.Speed = 200;
        missed.GetUnitAt(Target).Health.CurrentHp = 100;
        Check(!Cast(missed.GetUnitAt(Caster), 100401).Hit && missed.GetUnitAt(Target).IsAlive, "missed beam does not execute");
        var hp = missed.GetUnitAt(Target).Health;
        hp.MaxHp = 1400; hp.CurrentHp = 210;
        Check(!missed.GetUnitAt(Target).BelowJudgmentThreshold(15), "exact 15% stays safe despite floating-point percentage rounding");
        hp.CurrentHp = 209.99f;
        Check(missed.GetUnitAt(Target).BelowJudgmentThreshold(15), "strictly below fractional threshold qualifies");
    }
    static void CheckBeamTargetSelection()
    {
        foreach (bool enemyCaster in new[] { false, true })
        {
            using var battle = Create(enemyCaster);
            var caster = battle.GetUnitAt(enemyCaster ? Target : Caster);
            var adjacent = battle.GetUnitAt(enemyCaster ? Caster : Target);
            var low = battle.GetUnitAt(enemyCaster ? Friend : 15);
            adjacent.Health.MaxHp = 10000;
            adjacent.Health.CurrentHp = 600; // Lower percentage, but higher remaining HP.
            low.Health.CurrentHp = 500;
            var result = Cast(caster, 100401);
            Check(result.Target == low, "both sides choose lowest absolute HP instead of nearby target or lowest HP ratio");
            Near(adjacent.CurrentHp, 600, "nearby higher-HP enemy is untouched");
        }
        using (var battle = Create())
        {
            var caster = battle.GetUnitAt(Caster);
            var nearest = battle.GetUnitAt(Target);
            var far = battle.GetUnitAt(15);
            nearest.Health.CurrentHp = 600; far.Health.CurrentHp = 100;
            Check(caster.Skills.FindJudgmentBeamTarget(1) == nearest, "out-of-range low HP is excluded");
            nearest.Health.CurrentHp = 100;
            Check(caster.Skills.FindJudgmentBeamTarget(99) == nearest, "equal HP prefers nearest target");
            var tie = battle.GetUnitAt(13);
            tie.Health.CurrentHp = 100;
            nearest.Enabled = false;
            Check(caster.Skills.FindJudgmentBeamTarget(99) == tie, "disabled target excluded; equal distance uses lower slot");
            tie.Health.ApplyDamage(1000);
            Check(caster.Skills.FindJudgmentBeamTarget(99) == far, "dead target excluded");
            Check(caster.Skills.FindJudgmentBeamTarget(0) == null, "empty range has no target");
        }
    }

    static void CheckProtection()
    {
        using var battle = Create();
        var caster = battle.GetUnitAt(Caster);
        var friend = battle.GetUnitAt(Friend);
        var protection = Cast(caster, 100402);
        Near(protection.ShieldGained, 220, "all allies including caster receive caster attack as shield");
        Near(friend.GetComponent<BattleShieldComponent>().Amount, 110, "ally shield uses caster attack");
        Check(friend.Defense == 30 && caster.Defense == 35 && battle.GetUnitAt(Target).GetComponent<BattleShieldComponent>() == null, "ten defense applies only to own side");
        Check(friend.Combat.Defense == 20 && caster.Combat.Defense == 25, "base combat stats are not overwritten");
        friend.GetComponent<BattleShieldComponent>().OnRoundStarted(50);
        Check(friend.Defense == 30, "blessing shield lasts until broken");
        Cast(caster, 100402);
        Near(friend.GetComponent<BattleShieldComponent>().Amount, 220, "shield values stack");
        Check(friend.Defense == 30, "defense bonus does not stack");
        friend.Health.ApplyDamage(150);
        Check(friend.Defense == 30 && friend.CurrentHp == 1000, "partially remaining blessing preserves defense");
        friend.Health.ApplyDamage(70);
        Check(friend.Defense == 20 && friend.GetComponent<BattleShieldComponent>() == null, "breaking final blessing removes defense immediately");
        Cast(caster, 100402);
        var enemy = battle.GetUnitAt(Target);
        enemy.Combat.Attack = 300;
        var hit = new BattleActionResult();
        enemy.Combat.ResolveDirectHit(friend, hit);
        Near(hit.Damage, 160, "effective defense is used before shield absorption");
        Check(friend.Defense == 20, "broken shield restores base defense on that hit");
        Cast(caster, 100402);
        caster.Health.Execute();
        Check(friend.Defense == 30, "gifted shield survives the caster's death");
    }
    static void CheckJudgment()
    {
        using var battle = Create();
        var caster = battle.GetUnitAt(Caster);
        var doomed = battle.GetUnitAt(Target);
        var boundary = battle.GetUnitAt(13);
        var later = battle.GetUnitAt(15);
        doomed.Health.CurrentHp = 149;
        doomed.AddPermanentShield(5000, 10);
        boundary.Health.CurrentHp = 150;
        later.Health.CurrentHp = 160;
        battle.GetUnitAt(Friend).Health.CurrentHp = 100;
        Cast(caster, 100403);
        Check(!doomed.IsAlive && doomed.GetComponent<BattleShieldComponent>() == null, "activation immediately executes through shields");
        Check(boundary.IsAlive && battle.GetUnitAt(Friend).IsAlive, "exact 15% and friendly low HP are not executed");
        later.Health.ApplyDamage(10);
        Check(later.IsAlive, "reaching exactly 15% does not execute");
        later.Health.ApplyDamage(1);
        Check(!later.IsAlive, "other damage immediately triggers judgment");
        var executions = battle.Round.DrainActions().Where(a => a.Kind == BattleActionKind.Execution).ToArray();
        Check(executions.Length == 2 && executions.All(a => a.Unit == caster && a.Hits.Single().Executed), "execution attribution and action results");
        foreach (int round in new[] { 1, 2, 3 })
        {
            using var timed = Create();
            Cast(timed.GetUnitAt(Caster), 100403);
            timed.Round.RoundNumber = round;
            timed.GetUnitAt(Caster).GetComponent<BattleJudgmentComponent>().OnRoundStarted(round);
            var victim = timed.GetUnitAt(Target);
            victim.Health.CurrentHp = 160;
            victim.Health.ApplyDamage(11);
            Check(victim.IsAlive == (round == 3), "judgment active only cast round and following round");
        }
        using var transition = Create();
        transition.GetComponent<BattleEnemyAIComponent>().Enabled = false;
        Cast(transition.GetUnitAt(Caster), 100403);
        transition.World.Update(1);
        Check(transition.Round.RoundNumber == 2 && transition.GetUnitAt(Caster).GetComponent<BattleJudgmentComponent>() != null, "round hook preserves second round");
        transition.Round.Phase = BattlePhase.Combat;
        transition.World.Update(1);
        Check(transition.Round.RoundNumber == 3 && transition.GetUnitAt(Caster).GetComponent<BattleJudgmentComponent>() == null, "round hook disposes expired judgment");
    }
    static void CheckLifecycleAndEnemy()
    {
        using var paused = Create();
        Cast(paused.GetUnitAt(Caster), 100403);
        paused.Enabled = false;
        paused.GetUnitAt(Target).Health.CurrentHp = 149;
        paused.World.Update(0);
        Check(paused.GetUnitAt(Target) != null, "paused battle does not execute");
        paused.Enabled = true;
        paused.World.Update(0);
        Check(paused.GetUnitAt(Target) == null, "resume detects low HP without waiting for an action");
        using var dead = Create();
        var source = dead.GetUnitAt(Caster);
        Cast(source, 100403);
        source.Health.Execute();
        var survivor = dead.GetUnitAt(Target);
        survivor.Health.CurrentHp = 160;
        survivor.Health.ApplyDamage(11);
        Check(survivor.IsAlive && source.GetComponent<BattleJudgmentComponent>() == null, "dead caster no longer executes");
        using var multiple = Create(secondCaster: true);
        Cast(multiple.GetUnitAt(Caster), 100403);
        Cast(multiple.GetUnitAt(10), 100403);
        multiple.GetUnitAt(Target).Health.CurrentHp = 160;
        multiple.GetUnitAt(Target).Health.ApplyDamage(11);
        Check(multiple.Round.DrainActions().Count(a => a.Kind == BattleActionKind.Execution) == 1, "multiple casters cannot execute the same target twice");
        using var enemy = Create(enemyCaster: true);
        var pool = enemy.GetComponent<BattleSkillPointComponent>();
        Check(enemy.GetUnitAt(Target).Skills.PendingSkillId != 0, "enemy AI preselects judicator skills");
        Cast(enemy.GetUnitAt(Target), 100402);
        Check(enemy.GetUnitAt(13).Defense == 30 && enemy.GetUnitAt(Caster).Defense == 20, "enemy blessing protects enemy side");
        enemy.GetUnitAt(Caster).Health.CurrentHp = 149;
        Cast(enemy.GetUnitAt(Target), 100403);
        Check(enemy.GetUnitAt(Caster) == null && pool.Current == 30 && pool.EnemyCurrent == 15, "enemy execution spends only enemy points");
    }
    static void CheckDamageTriggers()
    {
        using var bleedBattle = Create();
        Cast(bleedBattle.GetUnitAt(Caster), 100403);
        var bleeding = bleedBattle.GetUnitAt(Target);
        bleeding.Health.CurrentHp = 200;
        var bleed = bleeding.AddComponent<BattleBleedComponent>();
        bleed.RemainingTicks = 1; bleed.NextTickRound = 1; bleed.MaxHpRatio = .1f;
        bleed.OnRoundStarted(bleedBattle.Round);
        var events = bleedBattle.Round.DrainActions();
        Check(!bleeding.IsAlive && events.Count == 2 && events[0].Kind == BattleActionKind.Bleed && events[1].Kind == BattleActionKind.Execution,
            "DOT triggers execution, and notifications preserve damage-before-execution order");
        using var cloudBattle = Create();
        Cast(cloudBattle.GetUnitAt(Caster), 100403);
        cloudBattle.Round.ActionRemaining = 100;
        var victim = cloudBattle.GetUnitAt(Target);
        victim.Health.CurrentHp = 300;
        var source = cloudBattle.GetUnitAt(Friend);
        var cloud = source.AddComponent<BattleThundercloudComponent>();
        cloud.RemainingStrikes = 1; cloud.AttackMultiplier = 2; cloud.KillShieldMultiplier = 4; cloud.ShieldRounds = 2; cloud.Range = 99;
        // FixedRandom chooses the final sorted enemy; narrow the range to the nearest target.
        cloud.Range = 2;
        cloudBattle.World.Update(1);
        var strikes = cloudBattle.Round.DrainActions();
        Check(!victim.IsAlive && strikes.Count == 2 && strikes[0].Kind == BattleActionKind.Lightning && strikes[1].Kind == BattleActionKind.Execution,
            "lightning damage triggers ordered execution");
        Check(strikes[0].ShieldGained == 0 && source.GetComponent<BattleShieldComponent>() == null,
            "execution kill credit cannot grant the lightning caster a kill shield");
    }
    public static void Run()
    {
        CheckLiveConfig(); CheckBeamTargetSelection(); CheckBeam(); CheckProtection(); CheckJudgment(); CheckLifecycleAndEnemy(); CheckDamageTriggers();
        Console.WriteLine("PASS: Judicator live config/roster/CDs, 150% beam and strict 15% threshold, shield bypass execution, all-ally protection/defense/nonstack/break, immediate and triggered judgment, round expiry, pause/death, duplicate casters and enemy AI/budget.");
    }
}
