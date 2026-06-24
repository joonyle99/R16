using UnityEngine;

/// <summary>미션 생성·타이머 튜닝값. (추격자 화면 위치 튜닝은 PursuerPanel(UI)이 소유)</summary>
[CreateAssetMenu(fileName = "MissionConfig", menuName = "Rising Hook/Mission Config")]
public class MissionConfig : ScriptableObject
{
    [Header("룩어헤드")]

    [Tooltip("미션 생성 시 살펴볼 플레이어 위쪽 구간 높이(월드 단위)")] public float bandHeight = 20f;

    [Header("난이도 스케일 (높이 기준)")]

    [Tooltip("맵 끝(endPoint)에서 도달할 최대 난이도")] public int maxDifficulty = 10;
    [Tooltip("난이도 0의 총 요구 처치 수")] public int baseTotalKills = 3;
    [Tooltip("난이도 1당 총 요구 처치 수 증가")] public float killsPerDifficulty = 1f;
    public int maxTotalKills = 12;

    [Tooltip("난이도 0의 요구 타입 수")] public int baseTypes = 1;
    [Tooltip("난이도 1당 요구 타입 수 증가")] public float typesPerDifficulty = 0.5f;
    public int maxTypes = 3;

    [Header("타이머")]

    public float baseTimeLimit = 12f;
    [Tooltip("총 요구 처치 수에 비례해 가산되는 시간")] public float timeLimitPerKill = 2f;
    public float maxTimeLimit = 30f;
}
