using TMPro;
using System;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(TextMeshPro))]
public class DamagePopup : MonoBehaviour
{
    [SerializeField] private float _startScale = 1.8f;
    [SerializeField] private float _endScale = 0.9f;
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private float _riseDistance = 0.6f;
    [SerializeField] private float _animDuration = 0.25f;
    [SerializeField] private float _lifetime = 1.5f;

    private TextMeshPro _text;
    private Sequence _sequence;
    private Action _onRelease;

    public void Initialize()
    {
        _text = GetComponent<TextMeshPro>();
        _text.sortingLayerID = SortingLayer.NameToID("VFX");
    }

    public void SetReleaseAction(Action onRelease) => _onRelease = onRelease;

    public void Play(int amount, Vector3 worldPos)
    {
        transform.position = worldPos + _spawnOffset;
        transform.localScale = Vector3.one * _startScale;

        _text.text = amount.ToString();

        var endPos = transform.position + Vector3.up * _riseDistance;

        _sequence?.Kill();
        _sequence = DOTween.Sequence();
        _sequence.Join(transform.DOScale(_endScale, _animDuration).SetEase(Ease.OutQuad));
        _sequence.Join(transform.DOMove(endPos, _animDuration).SetEase(Ease.OutQuad));
        _sequence.AppendInterval(_lifetime - _animDuration);
        _sequence.OnComplete(() =>
        {
            _sequence = null;
            _onRelease?.Invoke();
        });
    }

    private void OnDisable()
    {
        _sequence?.Kill();
        _sequence = null;
    }
}
