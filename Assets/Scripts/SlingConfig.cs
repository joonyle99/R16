using UnityEngine;

[CreateAssetMenu(fileName = "SlingConfig", menuName = "Rising Hook/SlingConfig")]
public class SlingConfig : ScriptableObject
{
    [System.Serializable]
    public struct KickConfig
    {
        [Tooltip("벽 킥 속도")]
        public float speed;
        [Tooltip("벽 킥 각도: 0 = 벽 반대편 수평, 90 = 수직 위")]
        [Range(0f, 90f)] public float angle;
    }

    [Tooltip("슬링샷 추진력 적용되는 중력 가속도 (단위/초²)")] public float slingGravity = 30f;
    [Tooltip("지상 발사 속도")] public float groundSlingSpeed = 18f;
    [Tooltip("공중 발사 속도 (지상보다 빠름)")] public float airSlingSpeed = 24f;
    [Tooltip("콤보 러쉬 발사 속도")] public float comboRushSlingSpeed = 40f;
    [Tooltip("발사 시 부여되는 기본 추진력(예산) 지속 시간(초)")] public float slingTime = 0.3f;
    [Tooltip("벽 킥(예산 바운스) 1회당 추진력 예산에 더해지는 시간(초)")] public float bonusTime = 0.2f;
    [Tooltip("추진력 예산 소진 후에도 추진력 판정을 유지하는 유예 시간(초)")] public float propelledGraceDuration = 0.08f;
    [Tooltip("법선이 수평에서 이 각도 이내면 벽으로 판정")][Range(0f, 90f)] public float wallAngleThreshold = 10f;
    [Tooltip("조준 고도각(수평 기준)이 이 값 이하면 땅샷 — 포물선 대신 직선 발사")][Range(-90f, 90f)] public float groundShotMaxAngle = 10f;
    [Tooltip("땅샷 조준선 길이")] public float groundShotAimLineLength = 3f;
    [Tooltip("조준 중 적용되는 타임스케일 (작을수록 더 느려짐)")] public float aimSlowScale = 0.05f;
    [Tooltip("슬로우 유지 시간 (리얼타임 초)")] public float aimSlowHoldDuration = 1.5f;
    [Tooltip("정상 속도 복귀 페이드 시간 (리얼타임 초)")] public float aimSlowFadeDuration = 1.0f;
    [Tooltip("차지 없을 때 피드백용 타임스케일 정지 시간(초)")] public float noChargePauseDuration = 0.3f;
    [Tooltip("궤적 예측/충돌 판정에 쓰는 서클캐스트 반지름")] public float circleCastRadius = 0.4f;
    [Tooltip("벽 킥(예산 바운스)이 허용되기까지 이동해야 하는 최소 누적 거리")] public float minDistance = 2f;
    [Tooltip("예산 바운스 최대 횟수 (궤적 세그먼트 수와도 연동)")] public int maxBounces = 4;
    [Tooltip("예산 소진 후에도 벽에 닿으면 주어지는 추가 킥 횟수 (조준선엔 미표시)")] public int bonusKickCount = 1;
    [Tooltip("킥 불가 상태로 벽에 닿았을 때 밀려나는 속도 (벽 타고 떨어지는 그림 방지)")] public float wallRepelSpeed = 5f;
    [Tooltip("시작 차지 — 지상 발사도 차지를 소모하므로 기본 1개 보장")] public int baseCharges = 1;
    [Tooltip("궤적 총 거리가 이 값 미만이면 끝 화살표 숨김")] public float minArrowDistance = 1.5f;
    [Tooltip("콤보 러쉬 모드 지속 시간(초)")] public float comboRushDuration = 10f;
    [Tooltip("콤보 러쉬 발동 임계값 (이 콤보 수 이상이면 러쉬 발동)")] public int comboRushThreshold = 15;
    [Tooltip("연속 미션 클리어 N회 시 콤보 러쉬 발동")] public int comboRushMissionChain = 3;
    [Tooltip("콤보 러쉬 중 이 속도 이상 상승할 때만 파괴 (천천히 떨어지거나 앉아있으면 파괴 안 함)")] public float comboRushSmashUpSpeed = 0.1f;
    [Tooltip("콤보 러쉬 중 이 속도 이상 수평 이동할 때만 파괴")] public float comboRushSmashSideSpeed = 8f;
    [Tooltip("벽 킥 설정 (속도/각도)")] public KickConfig kick = new KickConfig { angle = 45f, speed = 50f };
}
