using System.Collections.Generic;
using Framework.ECS;

public sealed class BattleShieldLayer
{
    public float Amount { get; internal set; }
    public int ExpiresAtRound { get; internal set; }
}

[ComponentOf(typeof(BattleUnit))]
public sealed class BattleShieldComponent : Entity
{
    internal List<BattleShieldLayer> Layers { get; } = new List<BattleShieldLayer>();
    public float Amount
    {
        get { float value = 0; foreach (var layer in Layers) value += layer.Amount; return value; }
    }
}
