using Framework.ECS;

[ComponentOf(typeof(BattleUnit))]
public sealed class BattlePositionComponent : Entity, IAwake, IDestroy
{
    public int SlotIndex { get; internal set; } = -1;
}
