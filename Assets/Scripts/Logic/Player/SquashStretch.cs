using System;
using UnityEngine;
using DG.Tweening;

public enum ContactSurface { Ground, Wall }

public sealed class SquashStretch : MonoBehaviour
{
    [SerializeField] private float _contactOffset = 0.37f;    
    [SerializeField] private float _duration = 0.2f;
    [SerializeField] private Ease _ease = Ease.OutElastic;

    private Transform _pivot;
    private Transform _animator;
    private Vector3 _baseScale;
    private Tween _tween;

    private void OnDestroy() => _tween?.Kill();

    public void Initialize(Transform pivot, Transform animator)
    {
        _pivot = pivot;
        _animator = animator;
        _baseScale = pivot.localScale;
        _baseScale.x = Mathf.Abs(_baseScale.x); // 좌우 반전은 SetFacingDir가 Visual에서 처리하므로 Pivot 스케일은 항상 양수
    }

    // =========================================

    public void PlayStretch(Action onComplete = null) // 세로로 길쭉하게 펴짐 → 탄성 복원 (히트포즈 해제 순간)
    {
        Play(new Vector3(0.55f, 1.6f, 1f));
        _tween.OnComplete(() => onComplete?.Invoke());
    }
    public void HoldSquash() => Hold(new Vector3(1.6f,  0.5f, 1f)); // 발사 직전 예비동작: 납작 웅크림 유지
    public void HoldSoftSquash() => Hold(new Vector3(1.2f, 0.75f, 1f)); // 살짝 웅크림 유지
    public void HoldSideSquash() => Hold(new Vector3(0.55f, 1.5f, 1f)); // 벽 충돌: 가로로 짜부라진 자세 유지

    // =========================================

    private void Play(Vector3 ratio)
    {
        if (_pivot == null) return;

        _tween?.Kill();
        _pivot.localScale = Vector3.Scale(_baseScale, ratio);
        _tween = _pivot.DOScale(_baseScale, _duration).SetEase(_ease).SetUpdate(true);
    }

    private void Hold(Vector3 ratio)
    {
        if (_pivot == null) return;

        _tween?.Kill();
        _tween = _pivot.DOScale(Vector3.Scale(_baseScale, ratio), 0.1f).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void Restore()
    {
        if (_pivot == null) return;

        _tween?.Kill();
        _tween = _pivot.DOScale(_baseScale, _duration).SetEase(_ease).SetUpdate(true);
    }

    // =========================================

    public void SetContactSurface(ContactSurface surface)
    {
        // 스쿼시 기준점을 접촉면 쪽으로 붙인다.
        // Wall: SetFacingDir로 벽 반대 방향을 보게 한 뒤 호출되므로, 로컬 왼쪽(등 뒤) = 벽 쪽
        var snapped = surface switch
        {
            ContactSurface.Wall   => Vector3.left,
            ContactSurface.Ground => Vector3.down,
            _                     => Vector3.zero
        };

        _pivot.localPosition = snapped * _contactOffset;
        _animator.localPosition = -snapped * _contactOffset;
    }
}
