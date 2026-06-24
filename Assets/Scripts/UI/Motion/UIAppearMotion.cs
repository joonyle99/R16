using System;
using DG.Tweening;
using UnityEngine;

public class UIAppearMotion : UIMotion
{
    [Header("Enter")]
    [SerializeField] private bool _enterUsePosition = false;
    [SerializeField] private float _enterPositionDelay = 0f;
    [SerializeField] private float _enterPositionDuration = 2f;
    [SerializeField] private Ease _enterPositionEase = Ease.OutElastic;
    [Space]
    [SerializeField] private bool _enterUseScale = false;
    [SerializeField] private float _enterScaleDelay = 0f;
    [SerializeField] private float _enterScaleDuration = 1f;
    [SerializeField] private Ease _enterScaleEase = Ease.OutCubic;
    [Space]
    [SerializeField] private bool _enterUseRotation = false;
    [SerializeField] private float _enterRotationDelay = 0f;
    [SerializeField] private float _enterRotationDuration = 2f;
    [SerializeField] private Ease _enterRotationEase = Ease.OutCubic;
    [SerializeField] private Vector3 _enterTargetRotation1 = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 _enterTargetRotation2 = new Vector3(0f, 0f, 0f);

    [Header("Exit")]
    [SerializeField] private bool _exitUsePosition = false;
    [SerializeField] private float _exitPositionDelay = 0f;
    [SerializeField] private float _exitPositionDuration = 0.4f;
    [SerializeField] private Ease _exitPositionEase = Ease.OutBack;
    [Space]
    [SerializeField] private bool _exitUseScale = false;
    [SerializeField] private float _exitScaleDelay = 0f;
    [SerializeField] private float _exitScaleDuration = 0.5f;
    [SerializeField] private Ease _exitScaleEase = Ease.InCubic;
    [Space]
    [SerializeField] private bool _exitUseRotation = false;
    [SerializeField] private float _exitRotationDelay = 0f;
    [SerializeField] private float _exitRotationDuration = 0.4f;
    [SerializeField] private Ease _exitRotationEase = Ease.InCubic;

    private Vector2 _originPos;
    private Vector3 _originScale;
    private Vector3 _originRotation;

    protected override void Awake()
    {
        base.Awake();
        
        _originPos = RectTransform.anchoredPosition;
        _originScale = RectTransform.localScale;
        _originRotation = RectTransform.localEulerAngles;
    }

    public override void Enter(Action onComplete = null)
    {
        sequence?.Kill();

        // !!를 조심하자.. not not 은 true 가 된다
        // e.g !! _enterUseRotation -> 회전만 활성화된 경우 return됨
        if (!_enterUsePosition && !_enterUseScale && ! _enterUseRotation)
        {
            onComplete?.Invoke();
            return;
        }

        sequence = DOTween.Sequence();

        if (_enterUsePosition)
        {
            RectTransform.anchoredPosition = Vector2.zero;
            sequence.Insert(_enterPositionDelay,
                RectTransform
                    .DOAnchorPos(_originPos, _enterPositionDuration)
                    .SetEase(_enterPositionEase));
        }

        if (_enterUseScale)
        {
            RectTransform.localScale = Vector3.zero;
            sequence.Insert(_enterScaleDelay,
                RectTransform
                    .DOScale(_originScale, _enterScaleDuration)
                    .SetEase(_enterScaleEase));
        }

        if (_enterUseRotation)
        {
            RectTransform.localEulerAngles = _enterTargetRotation1;
            sequence.Insert(_enterRotationDelay,
                RectTransform
                    .DOLocalRotate(_enterTargetRotation2, _enterRotationDuration, RotateMode.FastBeyond360)
                    .SetEase(_enterRotationEase));
        }

        sequence.OnComplete(() => onComplete?.Invoke());
    }

    public override void Exit(Action onComplete = null)
    {
        sequence?.Kill();

        if (!_exitUsePosition && !_exitUseScale && !_exitUseRotation)
        {
            onComplete?.Invoke();
            return;
        }
        
        sequence = DOTween.Sequence();

        if (_exitUsePosition)
        {
            RectTransform.anchoredPosition = _originPos;
            sequence.Insert(_exitPositionDelay,
                RectTransform
                    .DOAnchorPos(Vector2.zero, _exitPositionDuration)
                    .SetEase(_exitPositionEase));
        }

        // TODO: 이러한 코드 때문에 Enter와 Exit을 하나로 묶으만 안될 것 같다.
        // 다음에는 Appear, Disappear 분리하기
        // if (_exitUsePosition)
        // {
        //     RectTransform.anchoredPosition = _originPos;
        //     Sequence positionSeq = DOTween.Sequence();
        //     positionSeq.Append(RectTransform.DOAnchorPos(Vector2.zero, _exitPositionDuration * 0.5f).SetEase(_exitPositionEase));
        //     positionSeq.Append(RectTransform.DOAnchorPos(_originPos, _exitPositionDuration * 0.5f).SetEase(_exitPositionEase));
        //     sequence.Join(positionSeq);
        // }

        if (_exitUseScale)
        {
            RectTransform.localScale = _originScale;
            sequence.Insert(_exitScaleDelay,
                RectTransform
                    .DOScale(Vector3.zero, _exitScaleDuration)
                    .SetEase(_exitScaleEase));
        }

        if (_exitUseRotation)
        {
            RectTransform.localEulerAngles = _enterTargetRotation2;
            sequence.Insert(_exitRotationDelay,
                RectTransform
                    .DOLocalRotate(_enterTargetRotation1, _exitRotationDuration, RotateMode.FastBeyond360)
                    .SetEase(_exitRotationEase));
        }

        sequence.OnComplete(() => onComplete?.Invoke());
    }
}
