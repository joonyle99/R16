using System;
using DG.Tweening;
using UnityEngine;

public abstract class UIMotion : MonoBehaviour
{
    protected RectTransform rectTransform;
    protected RectTransform RectTransform => rectTransform ??= GetComponent<RectTransform>();
    protected Sequence sequence;

    protected virtual void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    protected virtual void OnDestroy()
    {
        sequence?.Kill();
    }

    public abstract void Enter(Action onComplete = null);
    public abstract void Exit(Action onComplete = null);

    public void Kill(bool complete = false) => sequence?.Kill(complete);
}
