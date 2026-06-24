using System;
using DG.Tweening;
using UnityEngine;

public class UIIdleMotion : UIMotion
{
    [SerializeField] private float _shakeInterval = 2f; // 흔들림 간격
    [SerializeField] private float _shakeAngle = 4f; // 흔들림 각도
    [SerializeField] private float _shakeDuration = 2f; // 흔들림 지속 시간
    [SerializeField] private int _shakeCount = 10; // 흔들림 사이클 수

    public override void Enter(Action onComplete = null)
    {
        sequence?.Kill();
        sequence = DOTween.Sequence();
        sequence.AppendInterval(_shakeInterval);

        // 사인 웨이브로 일정한 진폭의 진동 구현 (DOPunchRotation은 감쇠 진동이라 불균일)
        sequence.Append(DOVirtual.Float(0f, 1f, _shakeDuration, v =>
        {
            float angle = Mathf.Sin(v * _shakeCount * Mathf.PI * 2f) * _shakeAngle;
            RectTransform.localEulerAngles = Vector3.forward * angle;
        }).SetEase(Ease.Linear));

        sequence.SetLoops(-1);
        sequence.OnComplete(() => onComplete?.Invoke());
    }

    public override void Exit(Action onComplete = null)
    {
        sequence?.Kill();
        sequence = null;
        RectTransform.localEulerAngles = Vector3.zero;
        onComplete?.Invoke();
    }
}
