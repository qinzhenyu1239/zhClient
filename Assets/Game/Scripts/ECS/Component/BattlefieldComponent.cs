using System.Collections.Generic;
using Framework.ECS;

[ComponentOf(typeof(BattleEntity))]
public sealed class BattlefieldComponent : Entity, IAwake, IDestroy
{
    public int ColumnCount { get; internal set; }
    public int RowCount { get; internal set; }
    public int SlotCount => ColumnCount * RowCount;
    internal readonly List<BattleUnit> Members = new List<BattleUnit>();
    private IReadOnlyList<BattleUnit> view;
    public IReadOnlyList<BattleUnit> Units => view ?? (view = Members.AsReadOnly());
    internal BattleUnit[] Occupancy { get; set; }
}
