using System;
using DG.Tweening;
using UnityEngine;

public class ChargeAbsorbVfx : MonoBehaviour
{
    [SerializeField] private float _duration = 0.4f;
    [SerializeField] private float _arcHeight = 1.5f;
    [SerializeField] private Ease _ease = Ease.InQuad;

    private TrailRenderer _trailRenderer;
    private EffectParticle _chargeVfx;
    
    private Action _onRelease;
    private Tween _tween;

    private void Awake()
    {
        _trailRenderer = GetComponent<TrailRenderer>();
        _chargeVfx = GetComponentInChildren<EffectParticle>();
    }

    private void OnDisable()
    {
        _tween?.Kill();
    }

    public void SetEnabledTrail(bool enabled) => _trailRenderer.enabled = enabled;
    public void SetReleaseAction(Action onRelease) => _onRelease = onRelease;

    public void Play(Vector2 from, Func<Vector2> getTarget, Vector2 velocity, Action onArrived)
    {
        transform.position = from;
        _tween?.Kill();

        var start = from;
        var velDir = velocity.sqrMagnitude > 0.01f ? velocity.normalized : Vector2.up;
        var perp = new Vector2(-velDir.y, velDir.x); // velocity 수직 방향
        if (perp.y < 0f) perp = -perp;              // 위쪽 편향

        _tween = DOVirtual.Float(0f, 1f, _duration, t =>
        {
            var end = getTarget();
            var ctrl = (start + end) * 0.5f + perp * _arcHeight;
            transform.position = QuadBezier(start, ctrl, end, t);
        })
        .SetEase(_ease)
        .SetUpdate(true)
        .SetLink(gameObject)
        .OnComplete(() =>
        {
            onArrived?.Invoke();
            _onRelease?.Invoke();
            // if (_chargeVfx != null)
            // {
            //     _chargeVfx.SetReleaseAction(_onRelease);
            //     _chargeVfx.Play();
            // }
            // else
            // {
            //     _onRelease?.Invoke();
            // }
        });
    }

    private static Vector2 QuadBezier(Vector2 a, Vector2 ctrl, Vector2 b, float t)
    {
        var u = 1f - t;
        return u * u * a + 2f * u * t * ctrl + t * t * b;
    }
}
