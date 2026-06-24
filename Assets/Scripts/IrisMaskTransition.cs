using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 원형 SpriteMask 를 focus(플레이어) 위치에서 키우거나 줄여 화면을 열고/닫는 이리스 전환 연출.
/// 마스크 구조: 원 안쪽만 보이고 바깥은 검정 → 스케일이 클수록 화면이 열림.
/// </summary>
public class IrisMaskTransition : MonoBehaviour
{
    [SerializeField] private Transform _mask;
    [SerializeField] private float _openScale = 30f;
    [SerializeField] private float _duration = 0.45f;
    [SerializeField] private Ease _easeIn = Ease.OutCubic;
    [SerializeField] private Ease _easeOut = Ease.InCubic;
    [SerializeField] private float _delayTime = 0.5f;
    [SerializeField] private float _callbackDelayTime = 0.5f;

    private Transform _focus;
    private Tween _tween;

    private void LateUpdate()
    {
        if (_focus == null || _mask == null) return;
        Vector3 p = _focus.position;
        p.z = _mask.position.z;
        _mask.position = p; // 월드 추적
    }

    private void OnDestroy() => _tween?.Kill();

    public void Initialize(Transform focus)
    {
        _focus = focus;

        SetOpen();
    }

    /// <summary>
    /// 원을 확대해 화면을 연다 (씬 진입 시). 시작 상태를 닫힘으로 강제한 뒤 확대.
    /// </summary>
    public void TransitionIn(Action onComplete)
    {
        Play(0f, _openScale, _easeIn, _delayTime, 0f, onComplete);
    }

    /// <summary>
    /// 원을 축소해 화면을 닫는다 (씬 이탈 직전). 닫힘 후 hold 만큼 대기하고 콜백 호출.
    /// </summary>
    public void TransitionOut(Action onComplete)
    {
        Play(_openScale, 0f, _easeOut, _delayTime, _callbackDelayTime, onComplete);
    }

    private void Play(float from, float to, Ease ease, float delay, float hold, Action onComplete)
    {
        if (_mask == null) { onComplete?.Invoke(); return; }

        _tween?.Kill();
        SetScale(from);

        var seq = DOTween.Sequence()
            .SetUpdate(true) // Time.timeScale 영향 없이 동작
            .SetLink(gameObject);
        if (delay > 0f) seq.AppendInterval(delay);
        seq.Append(_mask.DOScale(to, _duration).SetEase(ease));
        if (hold > 0f) seq.AppendInterval(hold);
        seq.OnComplete(() => onComplete?.Invoke());
        _tween = seq;
    }

    /// <summary>화면을 즉시 연/닫힌 상태로 세팅 (연출 없이).</summary>
    public void SetOpen() => SetScale(_openScale);
    public void SetClosed() => SetScale(0f);
    private void SetScale(float s)
    {
        if (_mask != null) _mask.localScale = new Vector3(s, s, 1f);
    }
}
