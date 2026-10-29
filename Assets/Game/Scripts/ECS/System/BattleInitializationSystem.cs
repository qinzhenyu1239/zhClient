using System;
using System.Collections.Generic;
using Framework.ECS;

public static class BattleInitializationSystem
{
    public static BattleEntity CreateRandomBattle(IReadOnlyList<character> candidates, EcsWorld world = null,
        Random random = null, IEnumerable<skill> skillConfigs = null)
    {
        if (candidates == null || candidates.Count == 0)
            throw new ArgumentException("At least one formation character is required.", nameof(candidates));
        foreach (var candidate in candidates)
            if (candidate == null || candidate.hp <= 0)
                throw new ArgumentException("Formation characters must have positive HP.", nameof(candidates));
        random = random ?? new Random();
        var lineup = new Dictionary<int, character>();
        for (int slot = BattleEntity.UnitsPerSide; slot < BattleEntity.SlotCount; slot++)
            lineup.Add(slot, candidates[random.Next(candidates.Count)]);
        return CreateBattle(candidates[0], world, random, skillConfigs, lineup);
    }

    public static BattleEntity CreateBattle(character config, EcsWorld world = null, Random random = null, IEnumerable<skill> skillConfigs = null,
        IReadOnlyDictionary<int, character> lineup = null)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (config.hp <= 0) throw new ArgumentException("Expected a character with positive HP.", nameof(config));
        if (lineup != null)
            foreach (var entry in lineup)
                if (entry.Key < BattleEntity.UnitsPerSide || entry.Key >= BattleEntity.SlotCount || entry.Value == null || entry.Value.hp <= 0)
                    throw new ArgumentException("Invalid formation character or slot.", nameof(lineup));
        var definitions = new Dictionary<int, BattleSkillDefinition>();
        if (skillConfigs != null)
        {
            foreach (skill record in skillConfigs) definitions.Add(record.ID, new BattleSkillDefinition(record));
            var characters = new List<character> { config };
            if (lineup != null) characters.AddRange(lineup.Values);
            foreach (var record in characters)
                foreach (int id in new[] { record.skill1Id, record.skill2Id, record.skill3Id })
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
                character record = lineup != null && lineup.TryGetValue(slotIndex, out character selected) ? selected : config;
                BattleUnit unit = battle.AddChild<BattleUnit>();
                BattleIdentityComponent identity = unit.AddComponent<BattleIdentityComponent>();
                identity.UnitId = slotIndex + 1;
                identity.CharacterId = record.ID;
                identity.Name = record.name;
                identity.Side = slotIndex < BattleEntity.UnitsPerSide * 2 ? BattleSide.Ally : BattleSide.Enemy;
                unit.AddComponent<BattlePositionComponent>();
                BattleHealthComponent health = unit.AddComponent<BattleHealthComponent>();
                health.MaxHp = record.hp;
                health.CurrentHp = record.hp;
                BattleCombatComponent combat = unit.AddComponent<BattleCombatComponent>();
                combat.Attack = record.attack;
                combat.Defense = record.defense;
                combat.Speed = record.speed;
                combat.AttackRange = record.attackRange;
                combat.BattleType = record.battleType;
                BattleSkillComponent skills = unit.AddComponent<BattleSkillComponent>();
                skills.Skill1Id = record.skill1Id;
                skills.Skill2Id = record.skill2Id;
                skills.Skill3Id = record.skill3Id;
                foreach (int id in new[] { skills.Skill1Id, skills.Skill2Id, skills.Skill3Id })
                    if (definitions.TryGetValue(id, out BattleSkillDefinition definition)) skills.Definitions[id] = definition;
                unit.AddComponent<BattleActionComponent>();
                unit.AddComponent<BattleMovementComponent>();
                unit.AddComponent<BattleBasicAttackComponent>();
                field.Members.Add(unit);
                if (!unit.Position.TryPlace(slotIndex)) throw new InvalidOperationException("Invalid initial battle position.");
            }
            battle.AddComponent<BattleEnemyAIComponent>().PlanSkills();
            return battle;
        }
        catch { battle.Dispose(); throw; }
    }
}
