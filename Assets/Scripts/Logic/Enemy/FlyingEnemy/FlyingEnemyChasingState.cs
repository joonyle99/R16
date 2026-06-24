using UnityEngine;
using JoonyleGameDevKit;

public sealed class FlyingEnemyChasingState : StateBase<FlyingEnemyBehaviour>
{
    public override void Enter(FlyingEnemyBehaviour owner)
    {
        owner.PlayAnimation("Fly");
    }

    public override void Exit(FlyingEnemyBehaviour owner)
    {
        owner.Rigid.linearVelocity = Vector2.zero;
    }

    public override void Update(FlyingEnemyBehaviour owner, float deltaTime)
    {
        var player = owner.Player;

        if (player.IsDead)
        {
            owner.ChangeState<FlyingEnemyIdleState>();
            return;
        }

        var distance = Vector2.Distance(owner.transform.position, player.transform.position);
        if (distance > owner.LoseRange)
        {
            owner.ChangeState<FlyingEnemyIdleState>();
            return;
        }

        var toPlayer = (Vector2)player.transform.position - (Vector2)owner.transform.position;
        var isFacingRight = toPlayer.x > 0f;
        owner.SetFacingDir(isFacingRight);

        if (toPlayer.magnitude <= owner.StopRange)
        {
            owner.Rigid.linearVelocity = Vector2.zero;
            return;
        }

        owner.Rigid.linearVelocity = toPlayer.normalized * owner.ChaseSpeed;
    }
}
