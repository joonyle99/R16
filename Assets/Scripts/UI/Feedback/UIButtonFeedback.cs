using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public abstract class UIButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    protected Button button;
    public Button Button
    {
        get
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            return button;
        }
    }

    public bool Interactable { get; set; } = true;

    public bool HasButton => Button != null;

    protected virtual void Awake()
    {

    }
    protected virtual void Start()
    {

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!Interactable)
        {
            return;
        }

        if (HasButton && !Button.interactable)
        {
            return;
        }

        OnHoverEnter(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!Interactable)
        {
            return;
        }

        if (HasButton && !Button.interactable)
        {
            return;
        }

        OnHoverExit(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!Interactable)
        {
            return;
        }

        if (HasButton && !Button.interactable)
        {
            return;
        }

        OnPress(eventData);
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!Interactable)
        {
            return;
        }

        if (HasButton && !Button.interactable)
        {
            return;
        }

        OnRelease(eventData);
    }

    public abstract void OnHoverEnter(PointerEventData eventData);
    public abstract void OnHoverExit(PointerEventData eventData);
    public abstract void OnPress(PointerEventData eventData);
    public abstract void OnRelease(PointerEventData eventData);
}
