using Framework.ECS;

[ComponentOf(typeof(BattleUnit))]
public sealed class BattleAssassinComponent : Entity
{
    public bool HasInfiltrated { get; internal set; }
}
