using JoonyleGameDevKit;

public sealed class PursuerHitState : StateBase<PursuerActor>
{
    public override void Enter(PursuerActor owner) => owner.PlayAnimation(PursuerAnimationState.Hit);
    public override void Exit(PursuerActor owner) { }

    public override void Update(PursuerActor owner, float deltaTime)
    {
        var info = owner.Animator.GetCurrentAnimatorStateInfo(0);
        if (info.normalizedTime >= 1f && !owner.Animator.IsInTransition(0))
            owner.TransitionToTierState();
    }
}
