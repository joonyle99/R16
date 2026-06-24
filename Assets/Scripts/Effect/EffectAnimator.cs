using UnityEngine;

public class EffectAnimator : EffectBase, IAnimationEventHandler
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    protected override void OnPlay()
    {
        _animator.Rebind(); // default state로 되돌리고 즉시 0프레임부터 재생
        _animator.Update(0f);
    }

    protected override void OnStop() { }

    public void OnAnimationEvent(string eventName)
    {
        if (eventName == "Complete") OnComplete();
    }
}
