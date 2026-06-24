using DG.Tweening;
using UnityEngine;

public class PursuerPanel : UIPanel
{
    [SerializeField] private RectTransform _pursuerMarker;
    [SerializeField] private RectTransform[] _tierAnchors; // 인덱스 0 = 티어1, 2 = 티어3
    [SerializeField] private float _moveDuration = 0.3f;

    public void Initialize()
    {
        
    }

    public void SetTier(int tier) => UpdateMarker(tier, animate: true);

    private void UpdateMarker(int tier, bool animate)
    {
        var target = _tierAnchors[tier - 1].anchoredPosition;
        if (animate) _pursuerMarker.DOAnchorPos(target, _moveDuration).SetEase(Ease.OutCubic);
        else _pursuerMarker.anchoredPosition = target;
    }
}
