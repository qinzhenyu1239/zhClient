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
    internal Dictionary<int, long> CooldownReadyRounds { get; } = new Dictionary<int, long>();
    public int PendingSkillId { get; internal set; }
    public int ReservedCost { get; internal set; }
    internal int ReservedRound { get; set; }
    internal BattleSkillPointComponent ReservedPool { get; set; }
    internal BattleSide ReservedSide { get; set; }
}

// Immutable snapshots of table data; runtime buffs never modify shared configuration.
public sealed class BattleSkillDefinition
{
    public const int DoubleStrike = 100101;
    public const int Lifesteal = 100102;
    public const int ComboAttack = 100103;
    public const int ThunderStrike = 100201;
    public const int ChainLightning = 100202;
    public const int Thundercloud = 100203;
    public const int MoonCleave = 100301;
    public const int MoonBlink = 100302;
    public const int MoonShower = 100303;
    public const int JudgmentBeam = 100401;
    public const int HolyProtection = 100402;
    public const int HolyJudgment = 100403;
    public int Id { get; }
    public string Description { get; }
    public float Range { get; }
    public int PointCost { get; }
    public int CooldownRounds { get; }
    private readonly float[] parameters;

    public BattleSkillDefinition(skill source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (source.skillPointCost < 0 || source.skillCd < 0 || float.IsNaN(source.skillRange) || float.IsInfinity(source.skillRange) || source.skillRange < 0)
            throw new ArgumentException("Invalid skill cost, cooldown or range: " + source.ID);
        Id = source.ID;
        Description = source.description ?? string.Empty;
        Range = source.skillRange;
        PointCost = source.skillPointCost;
        CooldownRounds = source.skillCd;
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
            case ThunderStrike: Int(2); break;
            case ChainLightning: Int(1); Int(3); break;
            case Thundercloud: Int(1); Int(4); break;
            case MoonCleave:
            case MoonBlink: Value(1); break;
            case MoonShower:
                MoonTickCount(); Int(3); Value(4); Int(5);
                break;
            case JudgmentBeam:
                Value(1); ValidateExecutionThreshold(2);
                break;
            case HolyProtection:
                Value(1); Int(2);
                break;
            case HolyJudgment:
                ValidateExecutionThreshold(1); Int(2);
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

    private void ValidateExecutionThreshold(int index)
    {
        if (Value(index) > 100) throw new ArgumentException("Execution threshold must not exceed 100%.");
    }

    public float Percent(int index) => parameters[index - 1] / 100f;

    public float Value(int index)
    {
        float value = parameters[index - 1];
        if (value <= 0) throw new ArgumentException("Expected positive skill parameter " + index + ": " + Id);
        return value;
    }

    public int MoonTickCount()
    {
        double ticks = (double)Value(1) / Value(2);
        if (ticks < 1 || ticks > 10000 || Math.Abs(ticks - Math.Round(ticks)) > 0.0001)
            throw new ArgumentException("Moon shower duration must be a whole number of intervals.");
        return (int)Math.Round(ticks);
    }

    public string FormatDescription()
    {
        string text = Description;
        for (int i = 6; i >= 1; i--)
            text = text.Replace("param" + i, parameters[i - 1].ToString("0.##", CultureInfo.InvariantCulture));
        return text;
    }
}
