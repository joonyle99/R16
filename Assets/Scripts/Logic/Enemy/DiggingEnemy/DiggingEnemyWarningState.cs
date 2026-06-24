using JoonyleGameDevKit;

public sealed class DiggingEnemyWarningState : StateBase<DiggingEnemyBehaviour>
{
    private float _timer;

    public override void Enter(DiggingEnemyBehaviour owner)
    {
        _timer = 0f;
    }

    public override void Exit(DiggingEnemyBehaviour owner) { }

    public override void Update(DiggingEnemyBehaviour owner, float deltaTime)
    {
        _timer += deltaTime;

        if (_timer >= owner.WarningDuration)
            owner.ChangeState<DiggingEnemyEmergeState>();
    }
}
