using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonFuncFeedback : UIButtonFeedback
{
    private Action<bool> _actoin;

    public override void OnHoverEnter(PointerEventData eventData)
    {
        
    }

    public override void OnHoverExit(PointerEventData eventData)
    {
        
    }

    public override void OnPress(PointerEventData eventData)
    {
        _actoin?.Invoke(true);
    }

    public override void OnRelease(PointerEventData eventData)
    {
        _actoin?.Invoke(false);
    }

    public void SetFunc(Action<bool> action)
    {
        _actoin = action;
    }
}
