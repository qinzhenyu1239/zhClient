using System;
using System.Linq;
using System.Collections.Generic;

static class BloodLordChecks
{
    const int Leap=100601, Drain=100602, Domain=100603;
    sealed class FixedRandom : Random { public override int Next(int maxValue)=>maxValue-1; public override double NextDouble()=>.99; }
    static void Check(bool value,string message) { if(!value) throw new Exception("Blood lord: "+message); }
    static void Near(double actual,double expected,string message)=>Check(Math.Abs(actual-expected)<.001,message+" actual="+actual);
    static character Lord()=>new character {ID=1006,name="血裔领主",job="战士",hp=1200,attack=130,defense=25,speed=80,attackRange=1,battleType=1,skill1Id=Leap,skill2Id=Drain,skill3Id=Domain};
    static character Other()=>new character {ID=1001,name="fixture",hp=1000,attack=101,defense=40,speed=80,attackRange=1,battleType=1};
    static skill[] Definitions()=>new[]{
        new skill {ID=Leap,param1="150",skillRange=2,skillPointCost=5,skillCd=3},
        new skill {ID=Drain,param1="3",param2="20",skillRange=1,skillPointCost=5,skillCd=3},
        new skill {ID=Domain,param1="2",param2="50",param3="50",param4="20",param5="100",skillRange=99,skillPointCost=10,skillCd=5}
    };
    static int Slot(int original,bool enemy)=>enemy?23-original:original;
    static BattleEntity Create(bool enemy=false,bool secondLord=false)
    {
        var lineup=new Dictionary<int,character>();
        foreach(int slot in new[]{3,6,9,10,12,13,15,18}) lineup.Add(Slot(slot,enemy),slot==9 || (secondLord && slot==10)?Lord():Other());
        var battle=BattleInitializationSystem.CreateBattle(Lord(),random:new FixedRandom(),skillConfigs:Definitions(),lineup:lineup,fillEmptySlots:false);
        foreach(var unit in battle.Battlefield.Units) unit.Skills.CancelPendingSkill();
        return battle;
    }
    static BattleUnit Caster(BattleEntity battle,bool enemy=false)=>battle.GetUnitAt(Slot(9,enemy));
    static BattleActionResult Cast(BattleUnit unit,int id)
    {
        var battle=unit.GetParent<BattleEntity>();battle.Round.Phase=BattlePhase.Combat;
        Check(unit.Skills.TryQueue(id,out string reason),"queue "+id+": "+reason);
        var result=new BattleActionResult {Unit=unit,FromSlot=unit.SlotIndex,ToSlot=unit.SlotIndex,RoundNumber=battle.Round.RoundNumber};
        Check(unit.Skills.TryExecutePending(result),"cast "+id);return result;
    }
    static void Round(BattleEntity battle,int round)
    {
        battle.Round.RoundNumber=round;battle.GetComponent<BattleSkillPointComponent>().BeginRound(round);
        foreach(var unit in battle.Battlefield.Units) {unit.Action.HasActedThisRound=false;unit.Skills.CancelPendingSkill();unit.GetComponent<BattleBloodDomainComponent>()?.OnRoundStarted(round);}
    }
    static void CheckConfig()
    {
        var hero=ExcelDataMgr.Instance.GetConfigData<character>("1006");
        Check(hero.name=="血裔领主" && hero.hp>0 && hero.attack>0 && hero.battleType==1 && hero.attackRange==1,"live lord role and stats");
        var skills=ExcelDataMgr.Instance.GetConfigDatas<skill>(null);
        foreach(int id in new[]{Leap,Drain,Domain})
        {
            var d=new BattleSkillDefinition(skills.Single(s=>s.ID==id));
            Check(d.PointCost>0 && d.CooldownRounds>0 && !d.FormatDescription().Contains("param"),"live skill definitions/costs/CD/descriptions");
        }
        using var battle=BattleInitializationSystem.CreateRandomBattle(ExcelDataMgr.Instance.GetConfigDatas<character>(null),skillConfigs:skills);
        foreach(var side in new[]{BattleSide.Ally,BattleSide.Enemy}) Check(battle.Battlefield.Units.Count(u=>u.Side==side && u.CharacterId==1006)==1,"both random lineups include lord");
    }
    static void CheckLeap()
    {
        foreach(bool enemy in new[]{false,true})
        {
            using var battle=Create(enemy);var lord=Caster(battle,enemy);
            var blocked=battle.GetUnitAt(Slot(12,enemy));blocked.Health.CurrentHp=100;
            var target=battle.GetUnitAt(Slot(13,enemy));target.Health.CurrentHp=500;
            var outside=battle.GetUnitAt(Slot(18,enemy));outside.Health.CurrentHp=50;
            Check(lord.BloodLeapDestinations(blocked).Count==0,"surrounded low HP enemy cannot be selected");
            var action=Cast(lord,Leap);
            Check(action.Target==target && action.Hits.Count==1,"lowest remaining HP in two-cell range with a free adjacent cell is selected on both sides");
            Check(BattleMovementComponentSystem.GridDistance(lord.SlotIndex,target.SlotIndex,3)==1 && lord.CanEnterSlot(lord.SlotIndex),"leap lands in legal adjacent empty cell");
            Near(action.Damage,135,"leap deals 150 percent of normal attack damage");
            Near(blocked.CurrentHp,100,"blocked enemy untouched");Near(outside.CurrentHp,50,"out of range lower HP enemy untouched");
            Check(action.ToSlot==lord.SlotIndex && lord.Skills.CooldownRemaining(Leap)==3,"leap position snapshot and cooldown");
        }
        using(var battle=Create())
        {
            var lord=Caster(battle);var pool=battle.GetComponent<BattleSkillPointComponent>();
            Check(lord.Skills.TryQueue(Leap,out _) && pool.Current==25,"leap reservation uses normal points");
            foreach(var target in lord.Combat.FindEnemies(2,int.MaxValue)) target.Health.ApplyDamage(1000);
            battle.Round.Phase=BattlePhase.Combat;
            Check(!lord.Skills.TryExecutePending(new BattleActionResult()) && pool.Current==30 && lord.Skills.CooldownRemaining(Leap)==0,"lost leap target refunds without starting CD");
        }
    }
    static void CheckDrain()
    {
        foreach(bool enemy in new[]{false,true})
        {
            using var battle=Create(enemy);var lord=Caster(battle,enemy);lord.Health.CurrentHp=200;
            var first=battle.GetUnitAt(Slot(6,enemy));first.Health.CurrentHp=900;first.AddPermanentShield(1000,10);
            var second=battle.GetUnitAt(Slot(10,enemy));second.Health.CurrentHp=800;
            var third=battle.GetUnitAt(Slot(12,enemy));third.Health.CurrentHp=700;third.Combat.Speed=300;
            var fourth=battle.GetUnitAt(Slot(13,enemy));fourth.Health.CurrentHp=600;
            var far=battle.GetUnitAt(Slot(15,enemy));far.Health.MaxHp=10000;far.Health.CurrentHp=10000;
            var action=Cast(lord,Drain);
            Check(action.Hits.Select(h=>h.TargetSlot).SequenceEqual(new[]{first.SlotIndex,second.SlotIndex,third.SlotIndex}),"drain chooses highest current HP among eight neighbors, including allies");
            Near(first.CurrentHp,720,"ally loses twenty percent current HP");Near(second.CurrentHp,640,"second ally drained");Near(third.CurrentHp,560,"enemy drained regardless of evasion");
            Near(first.GetComponent<BattleShieldComponent>().Amount,1000,"health transfer bypasses shield");Near(fourth.CurrentHp,600,"fourth-highest excluded");Near(far.CurrentHp,10000,"distant highest HP excluded");
            Near(action.Healing,480,"heal equals actual health transferred");Near(lord.CurrentHp,680,"lord receives health");
            Near(lord.GetComponent<BattleDamageStatisticsComponent>().TotalDamage,140,"friendly health transfer excluded from enemy damage statistics");
            Round(battle,4);lord.Health.CurrentHp=1199;Cast(lord,Drain);Near(lord.CurrentHp,1200,"drain healing capped at own max HP");
        }
    }
    static void CheckDomainInheritance()
    {
        foreach(bool enemy in new[]{false,true})
        {
            using var battle=Create(enemy);var lord=Caster(battle,enemy);lord.Health.CurrentHp=100;
            Cast(lord,Domain);lord.Action.HasActedThisRound=true;
            int original=lord.SlotIndex;var friend=battle.GetUnitAt(Slot(6,enemy));int corpse=friend.SlotIndex;
            friend.Health.ApplyDamage(1000);
            Check(lord.SlotIndex==corpse && battle.GetUnitAt(original)==null && battle.GetUnitAt(corpse)==lord,"allied death immediately teleports lord after its action");
            Near(lord.Attack,180,"absorbs floor of half death attack");Near(lord.Defense,45,"absorbs half death defense");Near(lord.CurrentHp,300,"heals twenty percent of dead max HP");
            Check(lord.Combat.Attack==130 && lord.Combat.Defense==25,"inherited stats do not mutate base component values");
            friend.Health.ApplyDamage(1000);friend.Health.Execute();Near(lord.Attack,180,"dead unit cannot trigger twice");
            var victim=battle.GetUnitAt(Slot(12,enemy));var hit=new BattleActionResult();lord.Combat.ResolveDirectHit(victim,hit);
            Near(hit.Damage,140,"normal attack uses inherited attack");
            Round(battle,2);victim.Health.Execute();Near(lord.Attack,230,"enemy execution triggers next-round inheritance");Near(lord.Defense,65,"second inheritance stacks");
            battle.GetUnitAt(Slot(15,enemy)).Health.Execute();battle.GetUnitAt(Slot(18,enemy)).Health.Execute();Near(lord.Defense,100,"defense capped at one hundred");
            lord.AddPermanentShield(100,10);Near(lord.Defense,100,"defense cap includes shield defense bonuses");
            int beforeAttack=lord.Attack;lord.Health.CurrentHp=1190;
            battle.GetUnitAt(Slot(10,enemy)).Health.Execute();Near(lord.CurrentHp,1200,"domain healing capped at max HP");Check(lord.Attack>beforeAttack,"healing cap does not block stat absorption");
            int lastSlot=lord.SlotIndex;int lastAttack=lord.Attack;Round(battle,3);
            Check(lord.GetComponent<BattleBloodDomainComponent>()==null,"domain expires before third-round deaths");
            battle.GetUnitAt(Slot(3,enemy)).Health.Execute();Check(lord.SlotIndex==lastSlot && lord.Attack==lastAttack,"expired domain cannot react; inherited stats remain");
            Check(lord.HasActedThisRound==false,"passive reactions never consume next-round action");
        }
    }
    static void CheckDeathPathsAndIsolation()
    {
        using(var battle=Create())
        {
            var lord=Caster(battle);Cast(lord,Domain);var target=battle.GetUnitAt(12);int slot=target.SlotIndex;
            target.Health.CurrentHp=100;
            var bleed=target.AddComponent<BattleBleedComponent>();bleed.RemainingTicks=1;bleed.NextTickRound=1;bleed.MaxHpRatio=.2f;
            bleed.OnRoundStarted(battle.Round);
            Check(!target.IsAlive && lord.SlotIndex==slot && lord.Attack==180,"bleed death triggers domain");
            var events=battle.Round.DrainActions();Check(events.Count(e=>e.Kind==BattleActionKind.BloodDomain)==1,"one domain notification per death");
        }
        using(var battle=Create())
        {
            var lord=Caster(battle);Cast(lord,Domain);var judge=battle.GetUnitAt(3);
            var aura=judge.AddComponent<BattleJudgmentComponent>();aura.FirstRound=1;aura.LastRound=2;aura.ThresholdPercent=30;
            var target=battle.GetUnitAt(12);var attacker=battle.GetUnitAt(6);
            attacker.Combat.ResolveHit(target,new BattleActionResult(),800,true);
            Check(!target.IsAlive && lord.SlotIndex==12 && lord.Attack==180,"judgment execution triggered by damage reaches same death path");
        }
        using(var first=Create())
        using(var other=Create())
        {
            var lord=Caster(first);Cast(lord,Domain);
            other.GetUnitAt(6).Health.Execute();Near(lord.Attack,130,"death in another battle cannot leak inheritance");
            first.Enabled=false;first.GetUnitAt(6).Health.Execute();Near(lord.Attack,130,"paused battle cannot trigger domain");first.Enabled=true;
            lord.Health.Execute();Check(!lord.IsAlive && lord.GetComponent<BattleBloodDomainComponent>()==null,"dead lord cannot inherit itself or revive");
        }
        using(var battle=Create())
        {
            var lord=Caster(battle);Cast(lord,Domain);var enemy=battle.GetUnitAt(18);
            Check(enemy.Position.TryPlace(0),"enemy can occupy first column");enemy.Health.Execute();
            Check(lord.SlotIndex==0,"domain can teleport to any corpse including normally restricted own backline");
        }
    }
    static void CheckMultipleLordsAndAI()
    {
        using(var battle=Create(secondLord:true))
        {
            var first=battle.GetUnitAt(9);var second=battle.GetUnitAt(10);Cast(first,Domain);Cast(second,Domain);
            battle.GetUnitAt(6).Health.Execute();
            Near(first.Attack,180,"closer lord absorbs death");Near(second.Attack,130,"farther lord gets no inheritance");
            Check(first.SlotIndex==6 && second.SlotIndex==10 && battle.Battlefield.Units.Where(u=>u.IsAlive).Select(u=>u.SlotIndex).Distinct().Count()==7,
                "only nearest lord teleports and living units never overlap");
            var target=battle.GetUnitAt(13);
            first.Health.CurrentHp=100;second.Health.CurrentHp=100;
            target.Health.Execute();
            Near(first.Attack,180,"previous winner cannot absorb a farther death");
            Near(first.CurrentHp,100,"losing lord receives no healing");
            Near(second.Attack,180,"nearer second lord wins next corpse");
            Near(second.CurrentHp,300,"only winner receives healing");
        }
        using(var battle=Create())
        {
            var first=Caster(battle);
            var second=battle.GetUnitAt(12);
            second.Identity.CharacterId=1006;second.Combat.Attack=130;second.Combat.Defense=25;
            second.Skills.Skill3Id=Domain;
            second.Skills.Definitions[Domain]=new BattleSkillDefinition(Definitions().Single(s=>s.ID==Domain));
            Cast(first,Domain);Cast(second,Domain);
            battle.GetUnitAt(13).Health.Execute();
            Near(first.Attack,130,"cross-side farther lord loses");
            Near(second.Attack,180,"nearest opposing lord wins exclusively");
        }
        using(var battle=Create(true))
        {
            var lord=Caster(battle,true);var pool=battle.GetComponent<BattleSkillPointComponent>();
            battle.GetComponent<BattleEnemyAIComponent>().TrySelectSkill(lord);
            Check(lord.Skills.PendingSkillId==Domain && pool.Current==30 && pool.EnemyCurrent==20,"enemy AI can select domain with separate shared points");
            Cast(lord,Domain);lord.Skills.CooldownReadyRounds.Remove(Domain);
            battle.GetComponent<BattleEnemyAIComponent>().TrySelectSkill(lord);
            Check(lord.Skills.PendingSkillId!=Domain,"enemy AI does not overwrite active domain");
        }
    }
    public static void Run()
    {
        CheckConfig();CheckLeap();CheckDrain();CheckDomainInheritance();CheckDeathPathsAndIsolation();CheckMultipleLordsAndAI();
        Console.WriteLine("PASS: blood lord live roster/config, lowest-HP two-cell leap/free landing/150% basic damage, eight-neighbor top-three allied/enemy HP drain, domain deaths/teleports/inheritance/healing/defense cap/expiry/isolation and enemy AI.");
    }
}
