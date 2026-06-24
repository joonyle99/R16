using System.Collections.Generic;

/// <summary>한 미션의 요구 사항: 타입별 처치 수 + 제한 시간. (불변)</summary>
public sealed class MissionDefinition
{
    public readonly IReadOnlyDictionary<EnemyType, int> Requirements;
    public readonly float TimeLimit;
    public readonly int TotalRequired;

    public MissionDefinition(IReadOnlyDictionary<EnemyType, int> requirements, float timeLimit)
    {
        Requirements = requirements;
        TimeLimit = timeLimit;

        var total = 0;
        foreach (var kv in requirements) total += kv.Value;
        TotalRequired = total;
    }
}
