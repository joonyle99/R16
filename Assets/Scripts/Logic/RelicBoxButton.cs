using System;
using UnityEngine;

public enum RelicBoxAnimationState { Idle, Hit, Die }

public class RelicBoxButton : InteractableButton
{
    private static readonly int IDLE  = Animator.StringToHash("Idle");
    private static readonly int HIT   = Animator.StringToHash("Hit");
    private static readonly int DIE   = Animator.StringToHash("Die");

    private EyeTracker _eyeTracker;

    public override void Initialize(Action onInteracted)
    {
        base.Initialize(onInteracted);

        var target = FindFirstObjectByType<PlayerBehaviour>();
        _eyeTracker = GetComponentInChildren<EyeTracker>();
        _eyeTracker?.Initialize(target.Rigid);
        
        PlayAnimation(RelicBoxAnimationState.Idle);
    }

    public void Tick(float deltaTime)
    {
        _eyeTracker?.Tick(deltaTime);
    }

    public override void Interact()
    {
        if (interacted) return;

        var hasCoin = false;

        if (hasCoin)
        {
            interacted = true;
            PlayAnimation(RelicBoxAnimationState.Die);
            onInteracted?.Invoke();
        }
        else
        {
            PlayAnimation(RelicBoxAnimationState.Hit);
        }
    }

    public void PlayAnimation(RelicBoxAnimationState state)
    {
        switch (state)
        {
            case RelicBoxAnimationState.Idle:  PlayAnimation(IDLE);  break;
            case RelicBoxAnimationState.Hit:   PlayAnimation(HIT);   break;
            case RelicBoxAnimationState.Die:   PlayAnimation(DIE);   break;
        }
    }
}
