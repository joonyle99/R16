using UnityEngine;
using DG.Tweening;
using JoonyleGameDevKit;

public sealed class DiggingEnemyEmergeState : StateBase<DiggingEnemyBehaviour>
{
    private Sequence _sequence;

    public override void Enter(DiggingEnemyBehaviour owner)
    {
        var originPos = owner.Rigid.position;
        var emergePos = originPos + Vector2.up * owner.EmergeHeight;

        owner.PlayAnimation("Emerge");
        owner.Collider.enabled = true;

        _sequence?.Kill();
        _sequence = DOTween.Sequence();
        _sequence.SetLink(owner.gameObject); // 오브젝트 파괴 시 시퀀스 자동 Kill (파괴된 Rigidbody 접근 방지)
        var distance = Vector2.Distance(originPos, emergePos);
        _sequence.Append(owner.Rigid.DOMove(emergePos, distance / owner.EmergeSpeed).SetEase(owner.EmergeEase));
        _sequence.Append(owner.Rigid.DOMove(originPos, distance / owner.ReturnSpeed).SetEase(owner.ReturnEase));
        _sequence.OnComplete(() => owner.ChangeState<DiggingEnemyIdleState>());
    }

    public override void Exit(DiggingEnemyBehaviour owner)
    {
        _sequence?.Kill();
        owner.Collider.enabled = false;
    }

    public override void Update(DiggingEnemyBehaviour owner, float deltaTime) { }
}
