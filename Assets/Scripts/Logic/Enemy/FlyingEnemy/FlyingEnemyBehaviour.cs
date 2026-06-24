using UnityEngine;
using JoonyleGameDevKit;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class FlyingEnemyBehaviour : EnemyBehaviour
{
    public override EnemyType EnemyType => EnemyType.Flying;

    private StateMachine<FlyingEnemyBehaviour> _fsm;

    [SerializeField] private float _detectionRange = 5f;
    [SerializeField] private float _loseRange = 7f;
    [SerializeField] private float _chaseSpeed = 3f;
    [SerializeField] private float _stopRange = 1f;

    public float DetectionRange => _detectionRange;
    public float LoseRange => _loseRange;
    public float ChaseSpeed => _chaseSpeed;
    public float StopRange => _stopRange;

    protected override void OnInitialize()
    {
        _fsm = new StateMachine<FlyingEnemyBehaviour>(this);
        _fsm.AddState(new FlyingEnemyIdleState());
        _fsm.AddState(new FlyingEnemyChasingState());
        _fsm.ChangeState<FlyingEnemyIdleState>();
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

    public void ChangeState<TState>() where TState : StateBase<FlyingEnemyBehaviour> => _fsm.ChangeState<TState>();

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _loseRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _stopRange);
    }
#endif
}
