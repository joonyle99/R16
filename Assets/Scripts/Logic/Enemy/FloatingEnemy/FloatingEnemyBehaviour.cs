using DG.Tweening;
using UnityEngine;
using JoonyleGameDevKit;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class FloatingEnemyBehaviour : EnemyBehaviour
{
    public override EnemyType EnemyType => EnemyType.Floating;

    private StateMachine<FloatingEnemyBehaviour> _fsm;

    [SerializeField] private float _floatHeight = 1f;
    [SerializeField] private float _floatCycleDuration = 2f;
    [Range(0f, 1f)] [SerializeField] private float _cycleVariance = 0.2f;
    [SerializeField] private bool _moveHorizontal = false;
    [SerializeField] private Ease _floatEase = Ease.InOutExpo;
    [SerializeField] private LineRenderer _patrolLine;
    [SerializeField] private GameObject _lineMarkerPrefab;

    private float _actualCycleDuration;
    private readonly System.Collections.Generic.List<GameObject> _markers = new();

    public float FloatHeight => _floatHeight;
    public float FloatCycleDuration => _actualCycleDuration;
    public bool MoveHorizontal => _moveHorizontal;
    public Ease FloatEase => _floatEase;

    private void OnDestroy()
    {
        foreach (var marker in _markers)
            if (marker != null) Destroy(marker);
    }
    
    protected override void OnInitialize()
    {
        _actualCycleDuration = _floatCycleDuration * (1f + Random.Range(-_cycleVariance, _cycleVariance));

        _fsm = new StateMachine<FloatingEnemyBehaviour>(this);
        _fsm.AddState(new FloatingEnemyPatrolState());
        _fsm.ChangeState<FloatingEnemyPatrolState>();

        var center = transform.position;
        var direction = _moveHorizontal ? Vector3.right : Vector3.up;
        var pointA = center - direction * (_floatHeight * 0.5f);
        var pointB = center + direction * (_floatHeight * 0.5f);
        _patrolLine.positionCount = 2;
        _patrolLine.SetPosition(0, pointA);
        _patrolLine.SetPosition(1, pointB);

        if (_lineMarkerPrefab != null)
        {
            _markers.Add(Instantiate(_lineMarkerPrefab, pointA, Quaternion.identity, null));
            _markers.Add(Instantiate(_lineMarkerPrefab, pointB, Quaternion.identity, null));
        }
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

    public void ChangeState<TState>() where TState : StateBase<FloatingEnemyBehaviour> => _fsm.ChangeState<TState>();

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        var center = transform.position;
        var direction = _moveHorizontal ? Vector3.right : Vector3.up;
        var pointA = center - direction * (_floatHeight * 0.5f);
        var pointB = center + direction * (_floatHeight * 0.5f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pointA, pointB);
        Gizmos.DrawWireSphere(pointA, 0.15f);
        Gizmos.DrawWireSphere(pointB, 0.15f);
    }
#endif
}
