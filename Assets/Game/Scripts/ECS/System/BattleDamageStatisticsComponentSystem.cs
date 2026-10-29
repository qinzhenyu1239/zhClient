using Framework.ECS;

[EntitySystemOf(typeof(BattleDamageStatisticsComponent))]
public static class BattleDamageStatisticsComponentSystem
{
    public static void RecordDamage(this BattleUnit source, float actualHealthDamage)
    {
        if (source == null || source.IsDisposed || actualHealthDamage <= 0 ||
            float.IsNaN(actualHealthDamage) || float.IsInfinity(actualHealthDamage)) return;
        var statistics = source.GetComponent<BattleDamageStatisticsComponent>();
        if (statistics != null && !statistics.IsDisposed) statistics.TotalDamage += actualHealthDamage;
    }
}
