using System;
using System.Linq;

static class DamageStatisticsChecks
{
    static double Total(BattleUnit unit) => unit.GetComponent<BattleDamageStatisticsComponent>().TotalDamage;
    static void Near(double actual, double expected, string message)
    {
        if (Math.Abs(actual - expected) > .001) throw new Exception("Damage statistics: " + message + " actual=" + actual);
    }
    static BattleEntity Create() => BattleInitializationSystem.CreateBattle(new character {
        ID=1001, name="fixture", hp=1000, attack=100, defense=20, speed=80, attackRange=99 });
    public static void Run()
    {
        using (var battle = Create())
        {
            var attacker = battle.GetUnitAt(9);
            var target = battle.GetUnitAt(12);
            attacker.Combat.ResolveDirectHit(target, new BattleActionResult());
            Near(Total(attacker), 80, "direct damage subtracts defense");
            target.AddShield(150, 2);
            attacker.Combat.ResolveHit(target, new BattleActionResult(), 100, true);
            Near(Total(attacker), 80, "shield-only hit counts zero");
            attacker.Combat.ResolveHit(target, new BattleActionResult(), 100, true);
            Near(Total(attacker), 130, "only damage passing through shield counts");
            attacker.Combat.Speed = 0; target.Combat.Speed = 100;
            attacker.Combat.ResolveHit(target, new BattleActionResult(), 100);
            Near(Total(attacker), 130, "miss counts zero");
            attacker.Combat.ResolveHit(target, new BattleActionResult(), 99999, true);
            Near(Total(attacker), 1000, "overkill capped to remaining HP");
            var enemy = battle.GetUnitAt(13);
            enemy.Combat.ResolveHit(attacker, new BattleActionResult(), 2000, true);
            Near(Total(enemy), 1000, "enemy damage recorded");
            Near(Total(attacker), 1000, "dead unit retains total");
        }
        using (var battle = Create())
        {
            var source = battle.GetUnitAt(9);
            var target = battle.GetUnitAt(12);
            var judgment = battle.GetUnitAt(10);
            var aura = judgment.AddComponent<BattleJudgmentComponent>();
            aura.FirstRound = 1; aura.LastRound = 2; aura.ThresholdPercent = 15;
            battle.Round.Phase = BattlePhase.Combat;
            source.Combat.ResolveHit(target, new BattleActionResult(), 900, true);
            Near(Total(source), 900, "triggering damage credited once");
            Near(Total(judgment), 100, "execution credits remaining HP to judicator");
        }
        using (var battle = Create())
        {
            var source = battle.GetUnitAt(9);
            var target = battle.GetUnitAt(12);
            var bleed = target.AddComponent<BattleBleedComponent>();
            bleed.Source = source; bleed.NextTickRound = 1; bleed.RemainingTicks = 2; bleed.MaxHpRatio = .1f;
            target.AddShield(40, 2);
            source.Health.ApplyDamage(1000);
            bleed.OnRoundStarted(battle.Round);
            Near(Total(source), 60, "bleed credits dead caster and excludes shield");
            var replacement = battle.GetUnitAt(10);
            bleed.Source = replacement;
            battle.Round.RoundNumber = 2;
            bleed.OnRoundStarted(battle.Round);
            Near(Total(replacement), 100, "refreshed bleed credits current source");
            Near(Total(source), 60, "refresh does not duplicate old caster damage");
        }
        using (var battle = Create())
            foreach (var unit in battle.Battlefield.Units) Near(Total(unit), 0, "new battle resets all totals");
        Console.WriteLine("PASS: actual HP damage statistics, shields, misses, overkill, execution attribution, bleed attribution, dead units and reset.");
    }
}
