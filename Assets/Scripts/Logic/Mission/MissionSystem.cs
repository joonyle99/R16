using System;
using System.Collections.Generic;

/// <summary>
/// 현재 미션의 진행도와 타이머를 관리하는 순수 C# 시스템.
/// 미션 생성은 외부(MissionGenerator)에 위임하고, 여기서는 집계·타이머·이벤트만 담당한다.
/// </summary>
public sealed class MissionSystem
{
    private readonly Dictionary<EnemyType, int> _progress = new();
    public IReadOnlyDictionary<EnemyType, int> Progress => _progress;

    public MissionDefinition Mission { get; private set; }
    public float Remaining { get; private set; }
    public bool IsActive => Mission != null && Remaining > 0f;

    public event Action<MissionDefinition> OnMissionStarted;
    public event Action<EnemyType, int> OnProgressChanged; // (type, killedCount)
    public event Action<float, float> OnTimerChanged; // (remaining, limit)
    public event Action OnProgressReset;
    public event Action<float> OnMissionComplete; // remainingRatio (0~1) — 조기 완료 보너스용
    public event Action OnMissionFailed;
    public event Action<int, int> OnChainChanged; // (current, max) — 연속 클리어 진행도
    public event Action OnChainThresholdReached; // 연속 클리어 N회 달성

    private int _chainCount;
    private readonly int _chainThreshold;

    public MissionSystem(int chainThreshold) => _chainThreshold = chainThreshold;

    public void Tick(float deltaTime)
    {
        if (Mission == null || Remaining <= 0f) return;

        Remaining = Math.Max(0f, Remaining - deltaTime);
        OnTimerChanged?.Invoke(Remaining, Mission.TimeLimit);

        if (Remaining <= 0f)
        {
            _chainCount = 0;
            OnChainChanged?.Invoke(0, _chainThreshold);
            OnMissionFailed?.Invoke();
        }
    }

    public void StartMission(MissionDefinition mission)
    {
        Mission = mission;
        Remaining = mission.TimeLimit;

        _progress.Clear();
        foreach (var kv in mission.Requirements) _progress[kv.Key] = 0;

        OnMissionStarted?.Invoke(mission);
        OnTimerChanged?.Invoke(Remaining, mission.TimeLimit);
        OnChainChanged?.Invoke(_chainCount, _chainThreshold);
    }

    /// <summary>추진 처치 1건 집계. 미션과 무관한 타입이거나 이미 충족된 타입은 무시.</summary>
    public void RecordKill(EnemyType type)
    {
        if (Mission == null) return;
        if (!Mission.Requirements.TryGetValue(type, out var required)) return;

        var killed = _progress.TryGetValue(type, out var k) ? k : 0;
        if (killed >= required) return;

        killed++;
        _progress[type] = killed;
        OnProgressChanged?.Invoke(type, killed);

        if (IsComplete())
        {
            _chainCount++;
            if (_chainCount >= _chainThreshold)
            {
                _chainCount = 0;
                OnChainChanged?.Invoke(0, _chainThreshold);
                OnChainThresholdReached?.Invoke();
            }
            else
            {
                OnChainChanged?.Invoke(_chainCount, _chainThreshold);
            }
            var ratio = Mission.TimeLimit > 0f ? Remaining / Mission.TimeLimit : 0f;
            OnMissionComplete?.Invoke(ratio);
        }
    }

    /// <summary>착지 등으로 진행도(처치 수) 상실. 미션·타이머는 유지.</summary>
    public void ResetProgress()
    {
        if (Mission == null) return;

        var any = false;
        foreach (var kv in Mission.Requirements)
        {
            if (_progress.TryGetValue(kv.Key, out var killed) && killed != 0)
            {
                _progress[kv.Key] = 0;
                any = true;
            }
        }

        if (any) OnProgressReset?.Invoke();
    }

    private bool IsComplete()
    {
        foreach (var kv in Mission.Requirements)
        {
            if (!_progress.TryGetValue(kv.Key, out var killed) || killed < kv.Value)
                return false;
        }

        return true;
    }
}
