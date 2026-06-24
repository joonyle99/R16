using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class UIPanel : MonoBehaviour
{
    protected CanvasGroup _canvasGroup;
    protected bool isInteractable = false;
    protected bool isInitialized = false;

    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }
    
    public void SetVisible(bool visible)
    {
        if (gameObject.activeSelf == visible) return;
                
        if (visible) Show();
        else Hide();
    }

    protected virtual void Show()
    {
        gameObject.SetActive(true);
        SetInteractable(true);
    }
    protected virtual void Hide()
    {
        SetInteractable(false);
        gameObject.SetActive(false);
    }

    public virtual void SetInteractable(bool value)
    {
        if (_canvasGroup == null) return;
        
        _canvasGroup.alpha = value ? 1f : 0f;
        _canvasGroup.blocksRaycasts = value;
        _canvasGroup.interactable = value;
        
        isInteractable = value;
    }
}
