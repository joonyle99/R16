using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIButtonOverlayFeedback : UIButtonFeedback
{
    [SerializeField] protected GameObject overlay;
    public GameObject Overlay
    {
        get
        {
            if (overlay == null)
            {
                overlay = transform.Find("Overlay")?.gameObject;
            }

            return overlay; 
        }
    }

    protected override void Start()
    {
        base.Start();

        RefreshOverlay();
    }

    public override void OnHoverEnter(PointerEventData eventData)
    {

    }

    public override void OnHoverExit(PointerEventData eventData)
    {

    }

    public override void OnPress(PointerEventData eventData)
    {
        Overlay?.SetActive(true);
    }
    public override void OnRelease(PointerEventData eventData)
    {
        Overlay?.SetActive(false);
    }

    public void RefreshOverlay(bool? force = null)
    {
        Overlay?.SetActive(force ?? (HasButton && !Button.interactable));
    }

    public void SyncOverlaySprite()
    {
        var buttonImage = GetComponent<Image>();
        var overlayImage = overlay.GetComponent<Image>();
        
        if (buttonImage != null && overlayImage != null
            && buttonImage.sprite != overlayImage.sprite)
        {
            overlayImage.sprite = buttonImage.sprite;
        }
    }

    public void SetOverlaySprite(Sprite sprite)
    {
        var overlayImage = overlay.GetComponent<Image>();

        if (overlayImage != null && overlayImage.sprite)
        {
            overlayImage.sprite = sprite;
        }
    }
}
