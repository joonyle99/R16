using System;
using DG.Tweening;
using UnityEngine;

public class GameStartButton : InteractableButton
{
    private static readonly int SLEEP = Animator.StringToHash("Sleep2");
    private static readonly int IDLE  = Animator.StringToHash("Idle");
    private static readonly int HIT   = Animator.StringToHash("Hit");

    [SerializeField] private float _pressDepth = 0.2f; // 눌리는 깊이 (로컬 Y)
    [SerializeField] private float _pressDuration = 0.08f;
    [SerializeField] private float _releaseDuration = 0.2f;

    public override void Initialize(Action onInteracted)
    {
        base.Initialize(onInteracted);
        
        PlayAnimation(PursuerAnimationState.Sleep);
    }

    public void Tick(float deltaTime)
    {
        
    }

    public override void Interact()
    {
        if (interacted) return;

        interacted = true;

        var target = visual != null ? visual : transform;
        var baseY = target.localPosition.y;

        Time.timeScale = 0.04f;

        PlayAnimation(PursuerAnimationState.Hit);

        DOTween.Sequence()
            .Append(target.DOLocalMoveY(baseY - _pressDepth, _pressDuration).SetEase(Ease.OutQuad))
            .Append(target.DOLocalMoveY(baseY, _releaseDuration).SetEase(Ease.OutBack))
            .SetLink(gameObject);

        onInteracted?.Invoke();
    }

    public void PlayAnimation(PursuerAnimationState state)
    {
        switch (state)
        {
            case PursuerAnimationState.Sleep: PlayAnimation(SLEEP); break;
            case PursuerAnimationState.Idle:  PlayAnimation(IDLE);  break;
            case PursuerAnimationState.Hit:   PlayAnimation(HIT);   break;
        }
    }
}
