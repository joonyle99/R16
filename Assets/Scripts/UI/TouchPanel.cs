using System;
using UnityEngine;
using UnityEngine.UI;

public class TouchPanel : UIPanel
{
    private Button _touchButton;

    private void OnDestroy()
    {
        _touchButton.onClick.RemoveAllListeners();
    }

    public void Initialize(Action onTouchClicked)
    {
        _touchButton = GetComponentInChildren<Button>();
        _touchButton.onClick.AddListener(() => onTouchClicked?.Invoke());
    }
}
