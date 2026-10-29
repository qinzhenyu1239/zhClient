using Framework.ECS;

[ComponentOf(typeof(BattleUnit))]
public sealed class BattleMoonMarkComponent : Entity
{
    public int Stacks { get; internal set; }
}
