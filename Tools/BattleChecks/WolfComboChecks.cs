using System;
using System.Linq;

static class WolfComboChecks
{
    sealed class FixedRandom : Random
    {
        public override int Next(int maxValue) => maxValue - 1;
        public override int Next(int minValue, int maxValue) => maxValue - 1;
        public override double NextDouble() => 0;
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Wolf combo: " + message);
    }

    static BattleEntity Create()
    {
        var hero = new character { ID = 1001, hp = 10000, attack = 100, defense = 0, speed = 100,
            attackRange = 1, battleType = 1, skill2Id = 100102, skill3Id = 100103 };
        return BattleInitializationSystem.CreateBattle(hero, random: new FixedRandom(), skillConfigs: new[]
        {
            new skill { ID = 100102, param1 = "25", param2 = "5", skillPointCost = 1, skillRange = 99, skillCd = 2 },
            new skill { ID = 100103, param1 = "3", param2 = "3", param3 = "5", skillPointCost = 3, skillRange = 1, skillCd = 4 }
        });
    }

    static BattleActionResult Cast(BattleUnit unit, int id)
    {
        Check(unit.Skills.TryQueue(id, out string reason), "queue: " + reason);
        var action = new BattleActionResult { Unit = unit, FromSlot = unit.SlotIndex, ToSlot = unit.SlotIndex };
        Check(unit.Skills.TryExecutePending(action), "execute queued skill");
        return action;
    }

    public static void Run()
    {
        var live = new BattleSkillDefinition(ExcelDataMgr.Instance.GetConfigData<skill>("100103"));
        Check(live.Int(1) == 3, "live ultimate grants three attacks");
        foreach (int slot in new[] { 9, 12 })
        {
            using var battle = Create();
            BattleUnit actor = battle.GetUnitAt(slot);
            BattleUnit target = battle.GetUnitAt(slot == 9 ? 12 : 9);
            battle.Round.Phase = BattlePhase.Combat;
            var cast = Cast(actor, 100103);
            Check(cast.Hits.Count == 0 && actor.GetComponent<BattleComboAttackComponent>().RemainingAttacks == 3,
                "ultimate cast does not spend an attack charge");
            battle.Round.RoundNumber = 2;
            battle.Round.Phase = BattlePhase.Preparation;
            battle.Round.PreparationRemaining = 15;
            battle.GetComponent<BattleSkillPointComponent>().BeginRound(2);
            foreach (BattleUnit unit in battle.Battlefield.Units)
                if (unit != actor && unit != target) unit.RemoveComponent<BattleActionComponent>();
            actor.Health.CurrentHp = 9000;
            Check(actor.Skills.TryQueue(100102, out _), "lifesteal can be selected during ultimate");
            battle.World.Update(15);
            battle.Round.ActionOrder.Remove(actor);
            battle.Round.ActionOrder.Insert(0, actor);
            battle.World.Update(1);
            var result = battle.Round.DrainActions().Single();
            Check(result.Unit == actor && result.Kind == BattleActionKind.Skill && result.SkillId == 100102 && result.Hits.Count == 5,
                "one action casts lifesteal then executes exactly one ultimate volley for both sides");
            Check(result.Healing == 125 && actor.CurrentHp == 9125 && target.CurrentHp == 9500,
                "new 25% lifesteal applies to every hit of the same volley");
            Check(actor.GetComponent<BattleComboAttackComponent>().RemainingAttacks == 2 && actor.Skills.CooldownRemaining(100102) == 2,
                "only one charge consumed and normal lifesteal cooldown starts");
            var extra = new BattleActionResult();
            actor.GetComponent<BattleBasicAttackComponent>().TryAttack(extra);
            Check(extra.Hits.Count == 0 && actor.GetComponent<BattleComboAttackComponent>().RemainingAttacks == 2,
                "same round cannot execute a second volley");
            actor.Action.HasActedThisRound = false;
            battle.Round.RoundNumber = 3;
            Check(!actor.Skills.CanQueue(100102, out _), "ultimate does not bypass second skill cooldown");
            actor.GetComponent<BattleBasicAttackComponent>().TryAttack(new BattleActionResult());
            battle.Round.RoundNumber = 4;
            battle.GetComponent<BattleSkillPointComponent>().BeginRound(4);
            var last = Cast(actor, 100102);
            Check(last.Hits.Count == 5 && last.Healing == 250 && actor.GetComponent<BattleComboAttackComponent>() == null,
                "second lifesteal stack applies before third volley and ultimate ends after three attacks");
        }

        using var waiting = Create();
        BattleUnit rear = waiting.GetUnitAt(3);
        waiting.Round.Phase = BattlePhase.Combat;
        Cast(rear, 100103);
        waiting.Round.RoundNumber = 10;
        waiting.GetComponent<BattleSkillPointComponent>().BeginRound(10);
        var noTarget = Cast(rear, 100102);
        Check(noTarget.Hits.Count == 0 && rear.GetComponent<BattleComboAttackComponent>().RemainingAttacks == 3,
            "no target and elapsed rounds preserve attack charges while lifesteal still applies");
        waiting.GetUnitAt(6).Health.ApplyDamage(10000);
        waiting.GetUnitAt(9).Health.ApplyDamage(10000);
        rear.Position.TryPlace(9);
        var silence = rear.AddComponent<BattleSilenceComponent>();
        silence.FirstRound = silence.LastRound = 10;
        var silenced = new BattleActionResult();
        rear.GetComponent<BattleBasicAttackComponent>().TryAttack(silenced);
        Check(silenced.Hits.Count == 0 && rear.GetComponent<BattleComboAttackComponent>().RemainingAttacks == 3,
            "silence prevents attack without consuming ultimate charges");

        using var ordinary = Create();
        ordinary.Round.Phase = BattlePhase.Combat;
        Check(Cast(ordinary.GetUnitAt(9), 100102).Hits.Count == 0, "outside ultimate lifesteal stays a standalone buff");
        Console.WriteLine("PASS: wolf next-three-attack ultimate, both sides lifesteal-before-volley, same-volley healing, one action/charge, second-skill CD, charge expiry, no target/silence retention, standalone lifesteal.");
    }
}
