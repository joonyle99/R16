using JoonyleGameDevKit;

public sealed class PursuerIdleState : StateBase<PursuerActor>
{
    public override void Enter(PursuerActor owner) => owner.PlayAnimation(PursuerAnimationState.Idle);
    public override void Exit(PursuerActor owner) { }
    public override void Update(PursuerActor owner, float deltaTime) { }
}
