using System;
using UnityEngine;

/// <summary>
/// 추격자 티어 상태 (순수 C#). 시각화는 PursuerPanel(UI)이 담당.
/// - 티어 범위: 1~3, 시작 티어: 2
/// - 미션 성공 시: successesNeeded번 연속 성공해야 티어 -1 (후퇴)
/// - 미션 실패 시: 성공 누적 초기화 후 티어 +1 (전진). 티어3에서 실패하면 OnCatched 발생
/// </summary>
public sealed class PursuerState
{
    // 티어가 낮을수록 추격자가 멀리 있음 (플레이어에게 유리)

    public const int MIN_TIER = 1; // 가장 멀리 있는 상태
    public const int MAX_TIER = 3; // 따라잡히기 직전 상태 (실패 시 OnCatched)
    public const int START_TIER = 2; // 중간에서 시작

    public int Tier { get; private set; } = START_TIER;

    public event Action<int, int> OnTierChanged;
    public event Action OnCatched; // 티어3에서 또 실패 = 따라잡힘

    private readonly int _toLowerTierNeeded;
    private int _missionCompleteCount;

    public PursuerState(int toLowerTierNeeded = 2)
    {
        _toLowerTierNeeded = toLowerTierNeeded;
    }

    public void OnMissionFailure()
    {
        _missionCompleteCount = 0;
        if (Tier >= MAX_TIER) { OnCatched?.Invoke(); return; }
        SetTier(Tier + 1);
    }

    public void OnMissionComplete()
    {
        _missionCompleteCount++;
        if (_missionCompleteCount < _toLowerTierNeeded) return;
        _missionCompleteCount = 0;
        SetTier(Math.Max(MIN_TIER, Tier - 1));
    }

    private void SetTier(int tier)
    {
        if (tier == Tier) return;
        var prev = Tier;
        Tier = tier;
        OnTierChanged?.Invoke(prev, Tier);
    }

#if UNITY_EDITOR
    public void CheatSetTier(int tier)
    {
        tier = Mathf.Clamp(tier, MIN_TIER, MAX_TIER);
        SetTier(tier);
    }
#endif
}
