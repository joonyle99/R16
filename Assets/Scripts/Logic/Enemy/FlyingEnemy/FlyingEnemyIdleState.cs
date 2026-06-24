using UnityEngine;
using JoonyleGameDevKit;

public sealed class FlyingEnemyIdleState : StateBase<FlyingEnemyBehaviour>
{
    public override void Enter(FlyingEnemyBehaviour owner)
    {
        owner.PlayAnimation("Fly");
    }

    public override void Exit(FlyingEnemyBehaviour owner) { }

    public override void Update(FlyingEnemyBehaviour owner, float deltaTime)
    {
        if (owner.Player == null) return;
        
        var distance = Vector2.Distance(owner.transform.position, owner.Player.transform.position);
        if (distance <= owner.DetectionRange)
            owner.ChangeState<FlyingEnemyChasingState>();
    }
}
