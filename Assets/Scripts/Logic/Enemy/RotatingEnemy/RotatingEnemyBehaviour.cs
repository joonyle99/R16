using UnityEngine;
using JoonyleGameDevKit;

/// <summary>회전 적의 타격 부위. 약점은 회전과 함께 돌므로 맞은 방향으로 판정한다.</summary>
public enum EnemyHitPart
{
    Weakpoint, // 속살: 처치 가능한 약점
    Armor,     // 껍질: 단단한 방어 부위
}

[RequireComponent(typeof(Rigidbody2D))]
public sealed class RotatingEnemyBehaviour : EnemyBehaviour
{
    public override EnemyType EnemyType => EnemyType.Rotating;

    private StateMachine<RotatingEnemyBehaviour> _fsm;

    [SerializeField] private float _rotateSpeed = 180f; // 초당 회전 각도(도)
    [Range(0f, 1f)] [SerializeField] private float _rotateSpeedVariance = 0.2f;
    [SerializeField] private bool _clockwise = true;
    [SerializeField] private bool _randomizeClockwise = true; // 켜면 초기화 시 회전 방향을 무작위로 정한다

    [Header("부위 판정")]
    [SerializeField] private float _armorArcCenter = 90f; // 껍질이 향하는 로컬 방향(도). 90 = 위쪽
    [Range(0f, 180f)] [SerializeField] private float _armorArcHalfWidth = 90f; // 껍질 호의 반폭(도)

    private float _actualRotateSpeed;
    private bool _actualClockwise;

    // 시계 방향이면 각도가 감소해야 하므로 부호를 뒤집는다
    public float AngularVelocity => _actualClockwise ? -_actualRotateSpeed : _actualRotateSpeed;

    /// <summary>공격자 위치 기준으로 맞은 부위를 판정한다. 껍질이 회전하므로 현재 회전각을 보정해 비교한다.</summary>
    public EnemyHitPart GetHitPart(Vector2 attackerPos)
    {
        Vector2 toAttacker = attackerPos - Rigid.position;
        float worldAngle = Mathf.Atan2(toAttacker.y, toAttacker.x) * Mathf.Rad2Deg;
        float localAngle = Mathf.DeltaAngle(Rigid.rotation, worldAngle); // 회전 보정: 적 로컬 프레임 기준 각도
        bool isArmor = Mathf.Abs(Mathf.DeltaAngle(_armorArcCenter, localAngle)) <= _armorArcHalfWidth;
        return isArmor ? EnemyHitPart.Armor : EnemyHitPart.Weakpoint;
    }

    protected override void OnInitialize()
    {
        _actualRotateSpeed = _rotateSpeed * (1f + Random.Range(-_rotateSpeedVariance, _rotateSpeedVariance));
        _actualClockwise = _randomizeClockwise ? Random.value < 0.5f : _clockwise;

        _fsm = new StateMachine<RotatingEnemyBehaviour>(this);
        _fsm.AddState(new RotatingEnemyIdleState());
        _fsm.ChangeState<RotatingEnemyIdleState>();
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        if (IsDead) return;

        base.FixedTick(fixedDeltaTime);

        _fsm?.FixedUpdate(fixedDeltaTime);
    }

    public override void Tick(float deltaTime)
    {
        if (IsDead) return;

        base.Tick(deltaTime);

        _fsm?.Update(deltaTime);
    }

    public void ChangeState<TState>() where TState : StateBase<RotatingEnemyBehaviour> => _fsm.ChangeState<TState>();

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        const float RADIUS = 1f;
        float baseAngle = transform.eulerAngles.z; // 현재 회전 보정 (MoveRotation도 반영됨)
        Vector3 center = transform.position;

        float left = baseAngle + _armorArcCenter - _armorArcHalfWidth;  // 껍질 호 시작 경계
        float right = baseAngle + _armorArcCenter + _armorArcHalfWidth; // 껍질 호 끝 경계

        // 껍질 영역(채움) — 채워지지 않은 나머지가 약점
        UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.15f);
        UnityEditor.Handles.DrawSolidArc(center, Vector3.forward, AngleToDir(left), _armorArcHalfWidth * 2f, RADIUS);

        // 경계 2개
        Gizmos.color = Color.red;
        Gizmos.DrawLine(center, center + AngleToDir(left) * RADIUS);
        Gizmos.DrawLine(center, center + AngleToDir(right) * RADIUS);
    }

    private static Vector3 AngleToDir(float degree)
    {
        float rad = degree * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
    }
#endif
}
