using Framework.ECS;

public sealed class BattleEntity : Entity
{
    public const int ColumnCount = 9;
    public const int RowCount = 3;
    public const int SlotCount = ColumnCount * RowCount;
    public const int UnitsPerSide = 9;
    public const int BloodWerewolfId = 1001;

    public BattlefieldComponent Battlefield => GetComponent<BattlefieldComponent>();
    public BattleRoundComponent Round => GetComponent<BattleRoundComponent>();
}
