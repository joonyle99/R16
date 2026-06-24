using UnityEngine;
using UnityEngine.UI;

public class EnemyIconSlot : MonoBehaviour
{
    private Image _icon;
    private Image _overlay;

    public void Initialize()
    {
        _icon = GetComponent<Image>();
        _overlay = transform.childCount > 0 ? transform.GetChild(0).GetComponent<Image>() : null;
        // if (_overlay != null) _overlay.color = new Color(0f, 0f, 0f, 100f / 255f);
    }

    public void Setup(Sprite icon)
    {
        if (_icon != null) _icon.sprite = icon;
        if (_overlay != null)
        {
            _overlay.sprite = icon;
            _overlay.gameObject.SetActive(false);
        }
    }

    public void SetActiveOverlay(bool isOverlay)
    {
        if (_overlay != null) _overlay.gameObject.SetActive(isOverlay);
    }
}
