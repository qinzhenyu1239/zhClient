using System;
using System.Collections.Generic;
using Framework.ECS;

public static class BattleInitializationSystem
{
    public static BattleEntity CreateBattle(character config, EcsWorld world = null, Random random = null, IEnumerable<skill> skillConfigs = null)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (config.ID != BattleEntity.BloodWerewolfId || config.hp <= 0)
            throw new ArgumentException("Expected the blood werewolf character with positive HP.", nameof(config));
        var definitions = new Dictionary<int, BattleSkillDefinition>();
        if (skillConfigs != null)
        {
            foreach (skill record in skillConfigs) definitions.Add(record.ID, new BattleSkillDefinition(record));
            foreach (int id in new[] { config.skill1Id, config.skill2Id, config.skill3Id })
                if (id != 0 && !definitions.ContainsKey(id)) throw new ArgumentException("Missing skill definition: " + id);
        }
        world = world ?? new EcsWorld();
        BattleEntity battle = world.Root.AddChild<BattleEntity>();
        try
        {
            BattlefieldComponent field = battle.AddComponent<BattlefieldComponent>();
            BattleRoundComponent round = battle.AddComponent<BattleRoundComponent>();
            round.Random = random ?? new Random();
            battle.AddComponent<BattleSkillPointComponent>();
            for (int slotIndex = BattleEntity.UnitsPerSide; slotIndex < BattleEntity.SlotCount; slotIndex++)
            {
                BattleUnit unit = battle.AddChild<BattleUnit>();
                BattleIdentityComponent identity = unit.AddComponent<BattleIdentityComponent>();
                identity.UnitId = slotIndex + 1;
                identity.CharacterId = config.ID;
                identity.Name = config.name;
                identity.Side = slotIndex < BattleEntity.UnitsPerSide * 2 ? BattleSide.Ally : BattleSide.Enemy;
                unit.AddComponent<BattlePositionComponent>();
                BattleHealthComponent health = unit.AddComponent<BattleHealthComponent>();
                health.MaxHp = config.hp;
                health.CurrentHp = config.hp;
                BattleCombatComponent combat = unit.AddComponent<BattleCombatComponent>();
                combat.Attack = config.attack;
                combat.Defense = config.defense;
                combat.Speed = config.speed;
                combat.AttackRange = config.attackRange;
                combat.BattleType = config.battleType;
                BattleSkillComponent skills = unit.AddComponent<BattleSkillComponent>();
                skills.Skill1Id = config.skill1Id;
                skills.Skill2Id = config.skill2Id;
                skills.Skill3Id = config.skill3Id;
                foreach (int id in new[] { skills.Skill1Id, skills.Skill2Id, skills.Skill3Id })
                    if (definitions.TryGetValue(id, out BattleSkillDefinition definition)) skills.Definitions[id] = definition;
                unit.AddComponent<BattleActionComponent>();
                unit.AddComponent<BattleMovementComponent>();
                unit.AddComponent<BattleBasicAttackComponent>();
                field.Members.Add(unit);
                if (!unit.Position.TryPlace(slotIndex)) throw new InvalidOperationException("Invalid initial battle position.");
            }
            return battle;
        }
        catch { battle.Dispose(); throw; }
    }
}
