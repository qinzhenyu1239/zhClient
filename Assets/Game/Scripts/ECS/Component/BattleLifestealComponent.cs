using Framework.ECS;

[ComponentOf(typeof(BattleUnit))]
public sealed class BattleLifestealComponent : Entity
{
    public int Stacks { get; internal set; }
    public int MaxStacks { get; internal set; }
    public float RatioPerStack { get; internal set; }
}
