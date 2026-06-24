using JoonyleGameDevKit;

public sealed class PursuerSleepState : StateBase<PursuerActor>
{
    public override void Enter(PursuerActor owner) => owner.PlayAnimation(PursuerAnimationState.Sleep);
    public override void Exit(PursuerActor owner) { }
    public override void Update(PursuerActor owner, float deltaTime) { }
}
