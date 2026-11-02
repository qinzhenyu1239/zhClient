using System;
using System.Linq;
using System.Collections.Generic;

static class HunterChecks
{
    const int Skirmish = 100501, Precision = 100502, Tempo = 100503;
    sealed class FixedRandom : Random
    {
        public override int Next(int maxValue) => maxValue - 1;
        public override double NextDouble() => .99;
    }
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Hunter: " + message);
    }
    static void Near(double actual, double expected, string message) => Check(Math.Abs(actual - expected) < .001, message + " actual=" + actual);
    static character Hunter() => new character { ID=1005, name="丛林猎手", job="射手", hp=10000, attack=100,
        defense=20, speed=80, attackRange=3, battleType=4, skill1Id=Skirmish, skill2Id=Precision, skill3Id=Tempo };
    static character Other() => new character { ID=1001, name="fixture", hp=10000, attack=100, defense=20, speed=80, attackRange=3, battleType=1 };
    static skill[] Definitions() => new[] {
        new skill { ID=Skirmish, param1="3", param2="2", skillRange=3, skillPointCost=5, skillCd=3 },
        new skill { ID=Precision, param1="2", param2="3", skillRange=99, skillPointCost=5, skillCd=4 },
        new skill { ID=Tempo, param1="3", param2="2", param3="50", skillRange=3, skillPointCost=10, skillCd=5 }
    };
    static BattleEntity Create(bool enemyHunter = false)
    {
        var h=Hunter(); var o=Other();
        var lineup = enemyHunter
            ? new Dictionary<int, character> {{12,h},{9,o},{6,o},{3,o},{15,o},{18,o},{13,o}}
            : new Dictionary<int, character> {{9,h},{12,o},{15,o},{18,o},{6,o},{3,o},{10,o}};
        var battle=BattleInitializationSystem.CreateBattle(h, random:new FixedRandom(), skillConfigs:Definitions(), lineup:lineup, fillEmptySlots:false);
        foreach(var unit in battle.Battlefield.Units) unit.Skills.CancelPendingSkill();
        return battle;
    }
    static BattleActionResult Cast(BattleUnit unit, int id)
    {
        var battle=unit.GetParent<BattleEntity>();
        battle.Round.Phase=BattlePhase.Combat;
        Check(unit.Skills.TryQueue(id,out string reason), "queue " + id + ": " + reason);
        var action=new BattleActionResult { Unit=unit, FromSlot=unit.SlotIndex, ToSlot=unit.SlotIndex, RoundNumber=battle.Round.RoundNumber };
        Check(unit.Skills.TryExecutePending(action),"cast " + id);
        return action;
    }
    static BattleActionResult Attack(BattleUnit unit)
    {
        var action=new BattleActionResult { Unit=unit, FromSlot=unit.SlotIndex, ToSlot=unit.SlotIndex };
        unit.GetComponent<BattleBasicAttackComponent>().TryAttack(action);
        return action;
    }
    static void Round(BattleEntity battle, int round)
    {
        battle.Round.RoundNumber=round;
        battle.GetComponent<BattleSkillPointComponent>().BeginRound(round);
        foreach(var unit in battle.Battlefield.Units) { unit.Action.HasActedThisRound=false; unit.Skills.CancelPendingSkill(); }
    }
    static void CheckLiveConfig()
    {
        var hero=ExcelDataMgr.Instance.GetConfigData<character>("1005");
        Check(hero.job=="射手" && hero.battleType==4 && hero.hp>0 && hero.attack>0 && hero.attackRange>=1,
            "live hunter role/stats/range");
        var all=ExcelDataMgr.Instance.GetConfigDatas<skill>(null);
        foreach(int id in new[]{Skirmish,Precision,Tempo})
        {
            var definition=new BattleSkillDefinition(all.Single(s=>s.ID==id));
            Check(definition.PointCost>0 && definition.CooldownRounds>0 && !definition.FormatDescription().Contains("param"),"live definitions/costs/CD/descriptions");
        }
        using var battle=BattleInitializationSystem.CreateRandomBattle(ExcelDataMgr.Instance.GetConfigDatas<character>(null), skillConfigs:all);
        foreach(var side in new[]{BattleSide.Ally,BattleSide.Enemy})
            Check(battle.Battlefield.Units.Count(u=>u.Side==side && u.CharacterId==1005)==1,"random roster includes hunter on both sides");
    }
    static void CheckBasicAndSkirmish()
    {
        foreach(bool enemy in new[]{false,true})
        {
            using var battle=Create(enemy);
            var hunter=battle.GetUnitAt(enemy?12:9);
            var basic=Attack(hunter);
            Check(basic.Hits.Count==1 && basic.Hits[0].IsArrow,"normal attack fires one arrow on both sides");
            Near(basic.Damage,80,"normal arrow subtracts defense");
            int from=hunter.SlotIndex;
            var action=Cast(hunter,Skirmish);
            int travel=BattleMovementComponentSystem.GridDistance(from,hunter.SlotIndex,3);
            Check(travel<=3 && hunter.CanEnterSlot(hunter.SlotIndex) && battle.GetUnitAt(hunter.SlotIndex)==hunter,"skirmish respects three-cell displacement and legal occupancy");
            var nearest=hunter.Combat.FindEnemies(99,1).Single();
            Near(BattleMovementComponentSystem.GridDistance(hunter.SlotIndex,nearest.SlotIndex,3),3,"skirmish maximizes spacing while retaining range");
            Check(action.Hits.Count==2 && action.Hits.All(h=>h.IsArrow),"skirmish fires exactly two basic arrows");
            Near(action.Damage,160,"two ordinary arrows use full basic damage");
            Check(action.ToSlot==hunter.SlotIndex && hunter.Skills.CooldownRemaining(Skirmish)==3,"skill movement snapshot and cooldown");
        }
        using(var battle=Create())
        {
            var hunter=battle.GetUnitAt(9);
            foreach(var enemy in hunter.Combat.FindEnemies(99,int.MaxValue)) enemy.Health.ApplyDamage(10000);
            Check(hunter.FindSkirmishSlot(3)==-1 && !hunter.Skills.CanQueue(Skirmish,out _),"no target means no displacement skill or lost points");
        }
        using(var battle=Create())
        {
            var hunter=battle.GetUnitAt(9);
            foreach(var unit in battle.Battlefield.Units) if(unit!=hunter && unit.Side==hunter.Side) unit.Health.ApplyDamage(10000);
            Check(hunter.Position.TryPlace(3),"move hunter to back formation");
            Check(!hunter.GetComponent<BattleMovementComponent>().TryMoveTowardEnemy(),"ranged hunter stays when enemy is exactly three cells away");
            Check(battle.GetUnitAt(18).Position.TryPlace(20) && battle.GetUnitAt(15).Position.TryPlace(17) &&
                battle.GetUnitAt(12).Position.TryPlace(14), "move all targets out of range");
            Check(hunter.GetComponent<BattleMovementComponent>().TryMoveTowardEnemy(),"hunter pursues like warrior when outside range");
            Check(BattleMovementComponentSystem.GridDistance(3,hunter.SlotIndex,3)==1 &&
                BattleMovementComponentSystem.GridDistance(hunter.SlotIndex,14,3)==4,"ordinary movement takes one cell closer to nearest target");
            Check(hunter.GetComponent<BattleAssassinComponent>()==null,"shooter has no assassin infiltration");
        }
    }
    static void CheckPrecision()
    {
        foreach(bool enemy in new[]{false,true})
        {
            using var battle=Create(enemy);
            var hunter=battle.GetUnitAt(enemy?12:9);
            var expected=battle.Battlefield.Units.Where(u=>u!=hunter && u.Side==hunter.Side)
                .OrderBy(u=>BattleMovementComponentSystem.GridDistance(hunter.SlotIndex,u.SlotIndex,3)).ThenBy(u=>u.SlotIndex).Take(2).ToArray();
            Cast(hunter,Precision);
            Check(hunter.HasPrecision() && expected.All(u=>u.HasPrecision()),"precision affects self plus nearest two allies");
            Check(battle.Battlefield.Units.Count(u=>u.HasPrecision())==3,"precision excludes enemies and farther allies");
            var target=hunter.Combat.FindEnemies(99,1).Single();
            target.Combat.Speed=200; hunter.Combat.Speed=0;
            Check(Attack(hunter).Hits.All(h=>h.Hit),"precision normal arrows hit at zero base hit chance");
            var buffed=expected[0];buffed.Combat.Speed=0;
            var skill=new BattleActionResult();buffed.Combat.ResolveDirectHit(target,skill,1.5f);
            Check(skill.Hit,"precision applies to allied skill hit resolution");
            var unbuffed=battle.Battlefield.Units.Single(u=>u.Side==hunter.Side && u!=hunter && !expected.Contains(u));
            unbuffed.Combat.Speed=0;
            var miss=new BattleActionResult();unbuffed.Combat.ResolveDirectHit(target,miss);
            Check(!miss.Hit,"unbuffed units still roll hit chance");
            Round(battle,3);Check(hunter.HasPrecision(),"precision includes cast round and following two rounds");
            Round(battle,4);Check(!hunter.HasPrecision(),"precision stops before fourth-round attacks");
            var buff=hunter.GetComponent<BattlePrecisionComponent>();buff.OnRoundStarted(4);
            Check(buff.IsDisposed,"expired buff component cleaned up");
        }
    }
    static void CheckTempo()
    {
        using(var battle=Create())
        {
            var hunter=battle.GetUnitAt(9);var target=battle.GetUnitAt(12);
            var first=Cast(hunter,Tempo);
            Check(first.Target==target && first.Hits.Count==3 && first.Hits.All(h=>h.IsArrow),"tempo immediately fires three arrows at nearest enemy");
            Near(first.Damage,120,"tempo halves actual basic damage after defense");
            Near(hunter.GetComponent<BattleDamageStatisticsComponent>().TotalDamage,120,"tempo damage included once in actual damage statistics");
            Check(target.Position.TryPlace(14),"move locked target while retaining range");
            Round(battle,2);var next=Attack(hunter);
            Check(next.Target==target && next.Hits.Count==5,"second round retains original target despite nearer enemy");
            Near(next.Damage,200,"five half-damage arrows");
            Round(battle,3);Check(Attack(hunter).Hits.Count==7,"third round has seven arrows");
            Check(target.Position.TryPlace(20),"locked target exits range");
            Round(battle,4);var switched=Attack(hunter);
            Check(switched.Target!=target && switched.Hits.Count==1 && hunter.GetComponent<BattleLethalTempoComponent>()==null,"switching target ends stack and restores ordinary attack");
            Near(switched.Damage,80,"switch restores full normal damage");
        }
        using(var battle=Create(true))
        {
            var hunter=battle.GetUnitAt(12);var target=battle.GetUnitAt(9);
            target.Health.CurrentHp=100;
            var killed=Cast(hunter,Tempo);
            Check(!target.IsAlive && killed.Hits.Count==3 && hunter.GetComponent<BattleLethalTempoComponent>()==null,"kill ends tempo without retargeting extra arrows");
            Near(killed.Damage,100,"overkill not counted in tempo damage");
            Check(Attack(hunter).Hits.Count==1,"next target gets ordinary attack");
        }
        using(var battle=Create())
        {
            var hunter=battle.GetUnitAt(9);Cast(hunter,Tempo);
            var silence=hunter.AddComponent<BattleSilenceComponent>();silence.FirstRound=2;silence.LastRound=2;
            Round(battle,2);Check(Attack(hunter).Hits.Count==0,"silence blocks tempo volley");
            Round(battle,3);Check(Attack(hunter).Hits.Count==7,"round scaling is not affected by skipped or silenced action");
            hunter.Health.ApplyDamage(10000);
            Check(hunter.GetComponent<BattleLethalTempoComponent>()==null,"caster death removes tempo");
        }
    }
    static void CheckRangeLossClearsTempo()
    {
        using var battle=Create();
        var hunter=battle.GetUnitAt(9);
        Cast(hunter,Tempo);
        var tempo=hunter.GetComponent<BattleLethalTempoComponent>();
        var target=tempo.Target;
        foreach(var other in hunter.Combat.FindEnemies(99,int.MaxValue)) if(other!=target) other.Health.ApplyDamage(10000);
        Check(target.Position.TryPlace(20),"move only remaining target beyond range");
        Check(tempo.IsDisposed && hunter.GetComponent<BattleLethalTempoComponent>()==null,
            "target movement outside range immediately clears tempo even with no replacement target");
        Round(battle,2);
        Check(Attack(hunter).Hits.Count==0,"no target in range means no attack");
        Check(hunter.Position.TryPlace(14),"bring hunter back within range");
        Round(battle,3);
        var returned=Attack(hunter);
        Check(returned.Target==target && returned.Hits.Count==1,"returning target does not restore old tempo stacks");
        Near(returned.Damage,80,"returning target takes full ordinary attack damage");
    }

    static void CheckBuffAndTempoInteraction()
    {
        using(var battle=Create())
        {
            var hunter=battle.GetUnitAt(9);
            var target=battle.GetUnitAt(12);
            Cast(hunter,Tempo);
            var tempo=hunter.GetComponent<BattleLethalTempoComponent>();
            Round(battle,2);
            Check(hunter.Skills.TryQueue(Precision,out _) && hunter.GetComponent<BattleLethalTempoComponent>()==tempo,
                "queuing a skill does not interrupt before successful execution");
            hunter.Skills.CancelPendingSkill();
            Check(!tempo.IsDisposed,"canceling an uncast skill preserves tempo");
            Cast(hunter,Precision);
            Check(tempo.IsDisposed && hunter.GetComponent<BattleLethalTempoComponent>()==null,
                "precision execution immediately interrupts tempo");
            target.Combat.Speed=200; hunter.Combat.Speed=0;
            Round(battle,3);
            Check(hunter.Position.TryPlace(11),"move hunter before retained-target attack");
            var volley=Attack(hunter);
            Check(volley.Hits.Count==1 && volley.Hits.All(h=>h.Hit),
                "after skill interruption precision applies to a single ordinary arrow");
            Near(volley.Damage,80,"skill interruption restores full ordinary damage");
            var ally=battle.GetUnitAt(6);
            hunter.Health.ApplyDamage(10000);
            Check(ally.HasPrecision(),"caster death does not remove precision already granted to allies");
        }
        using(var battle=Create())
        {
            var hunter=battle.GetUnitAt(9);
            Cast(hunter,Tempo);
            var target=hunter.GetComponent<BattleLethalTempoComponent>().Target;
            target.Health.ApplyDamage(10000);
            Round(battle,2);
            var action=Attack(hunter);
            Check(action.Target!=target && action.Hits.Count==1,"another unit killing the locked target restores ordinary shooting");
            Near(action.Damage,80,"external target death restores full damage");
        }
    }

    static void CheckSkillInterruptions()
    {
        foreach(bool enemy in new[]{false,true})
        {
            using var battle=Create(enemy);
            var hunter=battle.GetUnitAt(enemy?12:9);
            Cast(hunter,Tempo);
            var old=hunter.GetComponent<BattleLethalTempoComponent>();
            Round(battle,2);
            var skirmish=Cast(hunter,Skirmish);
            Check(old.IsDisposed && hunter.GetComponent<BattleLethalTempoComponent>()==null,
                "both sides clear tempo before skirmish displacement and shots");
            Check(skirmish.Hits.Count==2,"skirmish retains its own two arrows");
            Near(skirmish.Damage,160,"skirmish arrows use full damage after interrupting tempo");
            Round(battle,6);
            var restarted=Cast(hunter,Tempo);
            Check(restarted.Hits.Count==3,"recasting tempo starts a fresh three-arrow volley");
            var active=hunter.GetComponent<BattleLethalTempoComponent>();
            Check(hunter.Skills.TryQueue(Precision,out _),"queue skill to test unsuccessful execution");
            var silence=hunter.AddComponent<BattleSilenceComponent>();silence.FirstRound=6;silence.LastRound=6;
            Check(!hunter.Skills.TryExecutePending(new BattleActionResult()) && !active.IsDisposed,
                "failed uncast skill does not interrupt tempo");
            silence.Dispose();
            Round(battle,11);
            var recast=Cast(hunter,Tempo);
            Check(active.IsDisposed && recast.Hits.Count==3 && hunter.GetComponent<BattleLethalTempoComponent>()!=active,
                "recasting while active replaces old stacks with a fresh tempo");
        }
    }

    static void CheckSchedulingAndRefunds()
    {
        using(var battle=Create())
        {
            var hunter=battle.GetUnitAt(9);
            Check(hunter.Skills.TryQueue(Tempo,out _),"queue tempo during preparation");
            battle.World.Update(15);
            battle.Round.ActionOrder.Remove(hunter);
            battle.Round.ActionOrder.Insert(0,hunter);
            battle.World.Update(1);
            var actions=battle.Round.DrainActions();
            var result=actions.Single(a=>a.Unit==hunter);
            Check(result.Hits.Count==3 && hunter.HasActedThisRound,"one scheduler action grants three initial arrows without extra normal attack");
            Check(Attack(hunter).Hits.Count==0,"already acted hunter cannot fire again");
        }
        using(var battle=Create())
        {
            var hunter=battle.GetUnitAt(9);var pool=battle.GetComponent<BattleSkillPointComponent>();
            Check(hunter.Skills.TryQueue(Tempo,out _) && pool.Current==20,"same shared point reservation rules");
            foreach(var enemy in hunter.Combat.FindEnemies(99,int.MaxValue)) enemy.Health.ApplyDamage(10000);
            battle.Round.Phase=BattlePhase.Combat;
            Check(!hunter.Skills.TryExecutePending(new BattleActionResult()) && pool.Current==30 && hunter.Skills.CooldownRemaining(Tempo)==0,"invalidated target refunds points and does not start cooldown");
        }
        using(var battle=Create(true))
        {
            var hunter=battle.GetUnitAt(12);var pool=battle.GetComponent<BattleSkillPointComponent>();
            battle.GetComponent<BattleEnemyAIComponent>().TrySelectSkill(hunter);
            Check(hunter.Skills.PendingSkillId==Tempo && pool.Current==30 && pool.EnemyCurrent==20,"enemy AI can pick hunter skill using its own budget");
        }
    }
    public static void Run()
    {
        CheckLiveConfig();CheckBasicAndSkirmish();CheckPrecision();CheckTempo();CheckBuffAndTempoInteraction();CheckRangeLossClearsTempo();CheckSkillInterruptions();CheckSchedulingAndRefunds();
        Console.WriteLine("PASS: hunter live roster/config, ranged movement/arrows, three-cell skirmish/two shots, nearest-two allied precision/expiry, tempo 3/5/7 half-damage shots/lock/switch/death/silence, shared points/CD/refunds and enemy AI.");
    }
}
