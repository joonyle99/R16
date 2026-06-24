using UnityEngine;
using JoonyleGameDevKit;

public sealed class DiggingEnemyIdleState : StateBase<DiggingEnemyBehaviour>
{
    private float _timer;
    private bool _initialized;

    public override void Enter(DiggingEnemyBehaviour owner)
    {
        // 첫 진입에만 랜덤 위상을 줘서 동시에 솟아오르지 않게 흩뿌린다
        _timer = _initialized ? 0f : Random.Range(0f, owner.IdleInterval);
        _initialized = true;

        owner.PlayAnimation("Dig");
        owner.Collider.enabled = false;
    }

    public override void Exit(DiggingEnemyBehaviour owner) { }

    public override void Update(DiggingEnemyBehaviour owner, float deltaTime)
    {
        _timer += deltaTime;

        if (_timer >= owner.IdleInterval)
            owner.ChangeState<DiggingEnemyWarningState>();
    }
}
