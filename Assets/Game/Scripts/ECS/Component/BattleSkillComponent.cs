using Framework.ECS;
using System;
using System.Collections.Generic;
using System.Globalization;

[ComponentOf(typeof(BattleUnit))]
public sealed class BattleSkillComponent : Entity, IDestroy
{
    public int Skill1Id { get; internal set; }
    public int Skill2Id { get; internal set; }
    public int Skill3Id { get; internal set; }
    internal Dictionary<int, BattleSkillDefinition> Definitions { get; } = new Dictionary<int, BattleSkillDefinition>();
    public int PendingSkillId { get; internal set; }
    public int ReservedCost { get; internal set; }
    internal int ReservedRound { get; set; }
    internal BattleSkillPointComponent ReservedPool { get; set; }
}

// Immutable snapshots of table data; runtime buffs never modify shared configuration.
public sealed class BattleSkillDefinition
{
    public const int DoubleStrike = 100101;
    public const int Lifesteal = 100102;
    public const int ComboAttack = 100103;
    public int Id { get; }
    public string Description { get; }
    public float Range { get; }
    public int PointCost { get; }
    private readonly float[] parameters;

    public BattleSkillDefinition(skill source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (source.skillPointCost < 0 || float.IsNaN(source.skillRange) || float.IsInfinity(source.skillRange) || source.skillRange < 0)
            throw new ArgumentException("Invalid skill cost or range: " + source.ID);
        Id = source.ID;
        Description = source.description ?? string.Empty;
        Range = source.skillRange;
        PointCost = source.skillPointCost;
        string[] raw = { source.param1, source.param2, source.param3, source.param4, source.param5, source.param6 };
        parameters = new float[raw.Length];
        for (int i = 0; i < raw.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(raw[i])) continue;
            if (!float.TryParse(raw[i], NumberStyles.Float, CultureInfo.InvariantCulture, out parameters[i]) ||
                float.IsNaN(parameters[i]) || float.IsInfinity(parameters[i]) || parameters[i] < 0)
                throw new ArgumentException("Invalid skill parameter " + (i + 1) + ": " + source.ID);
        }
        switch (Id)
        {
            case DoubleStrike: Int(1); Int(2); break;
            case Lifesteal: Int(2); break;
            case ComboAttack:
                Int(1); Int(2); Int(3);
                if (Int(3) < Int(2)) throw new ArgumentException("Invalid combo hit range.");
                break;
        }
    }

    public int Int(int index)
    {
        float value = parameters[index - 1];
        if (value < 1 || value >= int.MaxValue || Math.Floor(value) != value)
            throw new ArgumentException("Expected a positive integer skill parameter " + index + ": " + Id);
        return (int)value;
    }

    public float Percent(int index) => parameters[index - 1] / 100f;

    public string FormatDescription()
    {
        string text = Description;
        for (int i = 6; i >= 1; i--)
            text = text.Replace("param" + i, parameters[i - 1].ToString("0.##", CultureInfo.InvariantCulture));
        return text;
    }
}
