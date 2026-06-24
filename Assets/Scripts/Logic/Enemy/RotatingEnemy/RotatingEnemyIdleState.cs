using JoonyleGameDevKit;

public sealed class RotatingEnemyIdleState : StateBase<RotatingEnemyBehaviour>
{
    public override void Enter(RotatingEnemyBehaviour owner) { }

    public override void Exit(RotatingEnemyBehaviour owner) { }

    public override void FixedUpdate(RotatingEnemyBehaviour owner, float fixedDeltaTime)
    {
        // 물리 회전으로 콜라이더까지 함께 돌린다
        var nextRotation = owner.Rigid.rotation + owner.AngularVelocity * fixedDeltaTime;
        owner.Rigid.MoveRotation(nextRotation);
    }

    public override void Update(RotatingEnemyBehaviour owner, float deltaTime) { }
}
