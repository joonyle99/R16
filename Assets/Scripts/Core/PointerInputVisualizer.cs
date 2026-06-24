using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public class PointerInputVisualizer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Image _pressMarker;
    [SerializeField] private Image _dragMarker;

    [SerializeField] private float _maxLeanOffset = 15f;
    [SerializeField] private float _leanSpeed = 6f;

    private IPointerInput _pointerInput;
    private Func<bool> _canAim;

    private Canvas _canvas;
    private RectTransform _canvasRect;

    private float _defaultPressScale;
    private float _targetPressScale;
    private float _defaultDragScale;

    private Vector2 _pressScreenPos;
    private Vector2 _baseTextCanvasPos;
    private Vector2 _basePressCanvasPos;
    private Vector2 _currLeanOffset;
    private bool _pressMarkerReady;

    private float _pressMarkerFill = 1f;

    private void OnDestroy()
    {
        if (_pointerInput == null) return;

        _pointerInput.OnPress -= OnPress;
        _pointerInput.OnRelease -= OnRelease;
    }

    public void Initialize(IPointerInput pointerInput, Func<bool> canAim)
    {
        _pointerInput = pointerInput;
        _canAim = canAim;

        _canvas = GetComponent<Canvas>();
        _canvasRect = GetComponent<RectTransform>();

        _defaultPressScale = _pressMarker.rectTransform.localScale.x;
        _targetPressScale = _defaultPressScale; // _defaultPressScale * 0.75f;

        _defaultDragScale = _dragMarker.rectTransform.localScale.x;

        _text.gameObject.SetActive(false);
        _pressMarker.gameObject.SetActive(false);
        _dragMarker.gameObject.SetActive(false);

        _pointerInput.OnPress += OnPress;
        _pointerInput.OnRelease += OnRelease;

        ApplyMarkerSize(_pressMarker.rectTransform, _pointerInput.DragThresholdScreenRadius);
    }

    private void InitPressMarker()
    {
        _pressMarkerReady = true;
        _pressScreenPos = _pointerInput.GetScreenPos;
        _currLeanOffset = Vector2.zero;

        SetPosByScreen(_text.rectTransform, _pressScreenPos);
        _text.rectTransform.anchoredPosition += new Vector2(0f, _pointerInput.DragThresholdScreenRadius * 1.5f / _canvas.scaleFactor);
        _baseTextCanvasPos = _text.rectTransform.anchoredPosition;

        SetPosByScreen(_pressMarker.rectTransform, _pressScreenPos);
        _basePressCanvasPos = _pressMarker.rectTransform.anchoredPosition;

        _pressMarker.gameObject.SetActive(true);
        _dragMarker.gameObject.SetActive(false);
    }

    public void Tick(float deltaTime)
    {
        if (!_pointerInput.IsPressed) return;

        if (_canAim != null && !_canAim())
        {
            _text.gameObject.SetActive(false);
            _pressMarker.gameObject.SetActive(false);
            _dragMarker.gameObject.SetActive(false);
            return;
        }

        if (!_pressMarkerReady) InitPressMarker();
        else if (!_pressMarker.gameObject.activeSelf) _pressMarker.gameObject.SetActive(true);

        var isDragging = _pointerInput.IsDragging;
        _text.gameObject.SetActive(!isDragging);

        var scale = (isDragging ? _targetPressScale : _defaultPressScale) * _pressMarkerFill;
        _pressMarker.rectTransform.localScale = Vector3.one * scale;

        // 드래그 방향으로 살짝 기울기
        var screenDelta = _pointerInput.GetScreenPos - _pressScreenPos;
        var canvasDelta = screenDelta / _canvas.scaleFactor;
        var targetLean = Vector2.ClampMagnitude(canvasDelta * 0.1f, _maxLeanOffset);
        _currLeanOffset = Vector2.zero; // Vector2.Lerp(_currLeanOffset, targetLean, deltaTime * _leanSpeed);

        _pressMarker.rectTransform.anchoredPosition = _basePressCanvasPos + _currLeanOffset;
        _text.rectTransform.anchoredPosition = _baseTextCanvasPos + _currLeanOffset;

        if (isDragging && screenDelta.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(screenDelta.y, screenDelta.x) * Mathf.Rad2Deg - 90f;
            _pressMarker.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        }
        else if (!isDragging)
        {
            _pressMarker.rectTransform.localEulerAngles = Vector3.zero;
        }

        SetPosByScreen(_dragMarker.rectTransform, _pointerInput.GetScreenPos);
        // UpdateDragMarkerStretch(deltaTime);
        _dragMarker.gameObject.SetActive(true);
    }

    private void OnPress(Vector2 worldPos)
    {
        if (_canAim != null && !_canAim()) return;
        InitPressMarker();
    }

    private void OnRelease(Vector2 worldPos)
    {
        _pressMarkerReady = false;
        _pressMarkerFill = 1f;
        _text.gameObject.SetActive(false);
        _pressMarker.gameObject.SetActive(false);
        _dragMarker.gameObject.SetActive(false);
    }

    private void SetPosByScreen(RectTransform rt, Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, _canvas.worldCamera, out var localPoint);
        rt.anchoredPosition = localPoint;
    }

    private void ApplyMarkerSize(RectTransform rt, float screenRadius)
    {
        var localDiameter = screenRadius * 2f / _canvas.scaleFactor;
        rt.sizeDelta = new Vector2(localDiameter, localDiameter);
    }
    
    public void SetPressMarkerFill(float t)
    {
        // _pressMarkerFill = t;
    }
}
