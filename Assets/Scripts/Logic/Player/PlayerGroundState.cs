using UnityEngine;
using JoonyleGameDevKit;

public sealed class PlayerGroundState : StateBase<PlayerBehaviour>
{
    public override void Enter(PlayerBehaviour owner)
    {
        owner.NotifyLanded();
        owner.SlingBehaviour.RestoreCharges();
        owner.SquashStretch.SetContactSurface(ContactSurface.Ground);
        if (!owner.IsStunned) owner.PlayPlayerAnimation(PlayerAnimationState.Idle);
        owner.Rigid.linearVelocity = Vector2.zero;
        owner.Muffler.SetWindEnabled(true);
        EffectManager.Instance?.Play(VfxType.Dust, owner.transform.position);
    }

    public override void Exit(PlayerBehaviour owner)
    {
        owner.Muffler.SetWindEnabled(false);
    }

    public override void Update(PlayerBehaviour owner, float deltaTime)
    {
        if (!owner.PlatformerSensor.IsGrounded)
        {
            owner.ChangeState<PlayerAirState>();
        }
    }
}
