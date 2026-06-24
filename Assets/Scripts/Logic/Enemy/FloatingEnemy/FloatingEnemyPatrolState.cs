using UnityEngine;
using DG.Tweening;
using JoonyleGameDevKit;

public sealed class FloatingEnemyPatrolState : StateBase<FloatingEnemyBehaviour>
{
    private Vector2 _originPos;
    private Vector2 _pointA;
    private Vector2 _pointB;
    private float _eTime;

    public override void Enter(FloatingEnemyBehaviour owner)
    {
        _originPos = owner.transform.position;
        var direction = owner.MoveHorizontal ? Vector2.right : Vector2.up;
        _pointA = _originPos - direction * (owner.FloatHeight * 0.5f);
        _pointB = _originPos + direction * (owner.FloatHeight * 0.5f);
        _eTime = Random.value;

        owner.Rigid.linearVelocity = Vector2.zero;
        owner.PlayAnimation("Patrol");
    }

    public override void Exit(FloatingEnemyBehaviour owner)
    {
        owner.Rigid.linearVelocity = Vector2.zero;
    }

    public override void FixedUpdate(FloatingEnemyBehaviour owner, float fixedDeltaTime)
    {
        _eTime += fixedDeltaTime / owner.FloatCycleDuration;
        var pingPong = Mathf.PingPong(_eTime, 1f);
        var smoothed = DOVirtual.EasedValue(0f, 1f, pingPong, owner.FloatEase);
        owner.Rigid.MovePosition(Vector2.Lerp(_pointA, _pointB, smoothed));
    }

    public override void Update(FloatingEnemyBehaviour owner, float deltaTime)
    {
        
    }
}
