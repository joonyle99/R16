using System;
using UnityEngine;

public abstract class InteractableButton : MonoBehaviour
{
    [SerializeField] protected Transform visual; // 눌림 연출 대상 (비우면 자기 자신)

    public Animator Animator { get; private set; }

    protected bool interacted;
    protected Action onInteracted;

    public virtual void Initialize(Action onInteracted)
    {
        this.onInteracted = onInteracted;
    }

    protected virtual void Awake()
    {
        Animator = GetComponentInChildren<Animator>();
    }

    public abstract void Interact();

    public void PlayAnimation(int stateHash, float crossFade = 0f) => Animator.CrossFade(stateHash, crossFade);
}
