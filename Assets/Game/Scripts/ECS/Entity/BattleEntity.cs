using Framework.ECS;

public sealed class BattleEntity : Entity
{
    public const int ColumnCount = 8;
    public const int RowCount = 3;
    public const int SlotCount = ColumnCount * RowCount;
    public const int UnitsPerSide = 9;
    public const int AllyStartSlot = RowCount;
    public const int EnemyStartSlot = AllyStartSlot + UnitsPerSide;
    public const int FormationEndSlot = EnemyStartSlot + UnitsPerSide;
    public const int BloodWerewolfId = 1001;
    public const int ThunderGodId = 1002;
    public const int MoonlightGoddessId = 1003;

    public BattlefieldComponent Battlefield => GetComponent<BattlefieldComponent>();
    public BattleRoundComponent Round => GetComponent<BattleRoundComponent>();
}
