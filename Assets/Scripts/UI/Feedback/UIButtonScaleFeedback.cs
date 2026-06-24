using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonScaleFeedback : UIButtonFeedback
{
    [SerializeField] private float _pointerDownScaleRatio = 0.92f;
    [SerializeField] private float _pointerUpScaleRatio = 1.05f;

    private float _pointerDownDuration = 0.07f;
    private float _pointerUpDuration = 0.12f;

    private Vector3 _originalScale;

    protected override void Start()
    {
        base.Start();

        _originalScale = transform.localScale;
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

    public override void OnHoverEnter(PointerEventData eventData)
    {
        
    }

    public override void OnHoverExit(PointerEventData eventData)
    {
        
    }

    public override void OnPress(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(_originalScale * _pointerDownScaleRatio, _pointerDownDuration).SetEase(Ease.OutQuad).SetUpdate(true);
    }
    public override void OnRelease(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(_originalScale * _pointerUpScaleRatio, _pointerUpDuration).SetEase(Ease.OutBack).SetUpdate(true).OnComplete(() =>
        {
            transform.DOKill();
            transform.DOScale(_originalScale, 0.08f).SetEase(Ease.OutQuad).SetUpdate(true);
        });
    }
}
