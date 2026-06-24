using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 다가오는 구간의 적 가용성을 보고 "항상 달성 가능한" 미션을 생성한다.
/// 난이도는 높이(진행도) 기준으로 외부에서 주입 — 빠른/느린 유저가 같은 높이에서 같은 난이도를 만나도록.
/// </summary>
public sealed class MissionGenerator
{
    private readonly MissionConfig _config;

    public MissionGenerator(MissionConfig config)
    {
        _config = config;
    }

    /// <param name="difficulty">높이/진행도 기반 0 이상 정수</param>
    /// <param name="available">다가오는 밴드 내 살아있는 적의 타입별 수</param>
    public MissionDefinition Generate(int difficulty, IReadOnlyDictionary<EnemyType, int> available)
    {
        var candidates = new List<EnemyType>();
        foreach (var kv in available)
            if (kv.Value > 0) candidates.Add(kv.Key);

        var requirements = new Dictionary<EnemyType, int>();
        if (candidates.Count == 0)
            return new MissionDefinition(requirements, _config.baseTimeLimit); // 가용 적 없음 → 빈 미션(안전장치)

        var typesWanted = Mathf.Clamp(
            _config.baseTypes + Mathf.FloorToInt(difficulty * _config.typesPerDifficulty),
            1, Mathf.Min(_config.maxTypes, candidates.Count));

        var totalWanted = Mathf.Clamp(
            _config.baseTotalKills + Mathf.FloorToInt(difficulty * _config.killsPerDifficulty),
            typesWanted, _config.maxTotalKills);

        // 가용량 많은 타입 우선 선택 (공정성 — 부족한 타입을 과하게 요구하지 않음)
        candidates.Sort((a, b) => available[b].CompareTo(available[a]));
        var chosen = candidates.GetRange(0, typesWanted);

        // 각 타입 1개 보장 후, 나머지를 라운드로빈으로 분배하되 타입별 가용량을 넘지 않음
        foreach (var t in chosen) requirements[t] = 1;
        var assigned = typesWanted;
        var progressed = true;
        while (assigned < totalWanted && progressed)
        {
            progressed = false;
            foreach (var t in chosen)
            {
                if (assigned >= totalWanted) break;
                if (requirements[t] >= available[t]) continue;
                requirements[t]++;
                assigned++;
                progressed = true;
            }
        }

        var timeLimit = Mathf.Min(
            _config.baseTimeLimit + assigned * _config.timeLimitPerKill,
            _config.maxTimeLimit);

        return new MissionDefinition(requirements, timeLimit);
    }
}