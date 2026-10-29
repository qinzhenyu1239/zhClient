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
        var heroes = new List<character>();
        var heroIds = new HashSet<int>();
        foreach (var candidate in candidates)
        {
            if (candidate == null || candidate.hp <= 0)
                throw new ArgumentException("Formation characters must have positive HP.", nameof(candidates));
            if (heroIds.Add(candidate.ID)) heroes.Add(candidate);
        }
        if (heroes.Count > BattleEntity.UnitsPerSide)
            throw new ArgumentException("Too many unique heroes for the formation.", nameof(candidates));
        random = random ?? new Random();
        var lineup = new Dictionary<int, character>();
        foreach (int start in new[] { BattleEntity.AllyStartSlot, BattleEntity.EnemyStartSlot })
        {
            var empty = new List<int>();
            for (int i = 0; i < BattleEntity.UnitsPerSide; i++) empty.Add(start + i);
            foreach (character hero in heroes)
            {
                int index = random.Next(empty.Count);
                lineup.Add(empty[index], hero);
                empty.RemoveAt(index);
            }
        }
        return CreateBattle(heroes[0], world, random, skillConfigs, lineup, fillEmptySlots: false);
    }

    public static BattleEntity CreateBattle(character config, EcsWorld world = null, Random random = null, IEnumerable<skill> skillConfigs = null,
        IReadOnlyDictionary<int, character> lineup = null, bool fillEmptySlots = true)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (config.hp <= 0) throw new ArgumentException("Expected a character with positive HP.", nameof(config));
        if (lineup != null)
            foreach (var entry in lineup)
                if (entry.Key < BattleEntity.AllyStartSlot || entry.Key >= BattleEntity.FormationEndSlot || entry.Value == null || entry.Value.hp <= 0)
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
            for (int slotIndex = BattleEntity.AllyStartSlot; slotIndex < BattleEntity.FormationEndSlot; slotIndex++)
            {
                if (!fillEmptySlots && (lineup == null || !lineup.ContainsKey(slotIndex))) continue;
                character record = lineup != null && lineup.TryGetValue(slotIndex, out character selected) ? selected : config;
                BattleUnit unit = battle.AddChild<BattleUnit>();
                BattleIdentityComponent identity = unit.AddComponent<BattleIdentityComponent>();
                identity.UnitId = slotIndex + 1;
                identity.CharacterId = record.ID;
                identity.Name = record.name;
                identity.Side = slotIndex < BattleEntity.EnemyStartSlot ? BattleSide.Ally : BattleSide.Enemy;
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
                if (record.battleType == 3) unit.AddComponent<BattleAssassinComponent>();
                BattleSkillComponent skills = unit.AddComponent<BattleSkillComponent>();
                skills.Skill1Id = record.skill1Id;
                skills.Skill2Id = record.skill2Id;
                skills.Skill3Id = record.skill3Id;
                foreach (int id in new[] { skills.Skill1Id, skills.Skill2Id, skills.Skill3Id })
                    if (definitions.TryGetValue(id, out BattleSkillDefinition definition)) skills.Definitions[id] = definition;
                unit.AddComponent<BattleDamageStatisticsComponent>();
                unit.AddComponent<BattleActionComponent>();
                unit.AddComponent<BattleMovementComponent>();
                unit.AddComponent<BattleBasicAttackComponent>();
                field.Members.Add(unit);
                if (!unit.Position.TryPlace(slotIndex)) throw new InvalidOperationException("Invalid initial battle position.");
                var assassin = unit.GetComponent<BattleAssassinComponent>();
                if (assassin != null)
                    assassin.OpeningInfiltrationEligible = slotIndex / field.RowCount ==
                        (unit.Side == BattleSide.Ally ? 1 : field.ColumnCount - 2);
            }
            battle.AddComponent<BattleEnemyAIComponent>().PlanSkills();
            return battle;
        }
        catch { battle.Dispose(); throw; }
    }
}
