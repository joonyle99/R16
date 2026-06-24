using UnityEngine;
using DG.Tweening;

public enum CameraMode { None, OutGame, InGame }

public class CameraController : MonoBehaviour
{
    private CameraMode _mode;

    private Camera _mainCamera;
    public Camera MainCamera => _mainCamera;

    // ======== 카메라 크기 ========

    public float CameraWidth => CameraHeight * CameraAspect;
    public float CameraHeight => _mainCamera.orthographicSize * 2f;
    public float CameraAspect => (float)Screen.width / (float)Screen.height;

    // ======== 아웃게임 카메라 ========

    [SerializeField] private float _roomMoveSpeed = 6f;
    [SerializeField] private float _floorMoveSpeed = 6f; // Practice 층 전환 시 Y 글라이드 속도

    // ======== 인게임 카메라 ========

    [SerializeField] private float _defaultVerticalOffset = 2f; // 기본 상태
    [SerializeField] private float _urgentVerticalOffset = 0f; // 긴박 상태 (추격자 근접 등)
    [SerializeField] private float _urgentDuration = 0.4f;
    [SerializeField] private Ease _urgentEaseIn = Ease.OutExpo; // 긴박 진입
    [SerializeField] private Ease _urgentEaseOut = Ease.InOutSine; // 긴박 해제
    [SerializeField] private float _riseSpeed = 8f;  // 플레이어 상승 시 카메라 추적 속도
    [SerializeField] private float _fallSpeed = 3f;  // 플레이어 하강 시 카메라 추적 속도
    private float _verticalOffset;
    private Transform _followTarget;
    private Tween _zoomTween;
    private Tween _offsetTween;
    private float _minY = float.NegativeInfinity; // 카메라가 이 월드 Y 아래로 내려가지 못하도록 막는 하한선 (역주행 방지)

    // =========== ... ===========

    private float _fixedX;
    private float _fixedY;
    private float _baseOrthoSize;
    private CameraShaker _shaker;

    public void LateTick(float deltaTime)
    {
        if (_mode == CameraMode.OutGame)
        {
            var pos = transform.position;
            pos.x = Mathf.Lerp(pos.x, _fixedX, deltaTime * _roomMoveSpeed);
            pos.y = Mathf.Lerp(pos.y, _fixedY, deltaTime * _floorMoveSpeed); // 목표가 안 바뀌면 곧 멈춤
            pos += (Vector3)_shaker.LateTick(Time.unscaledDeltaTime);
            transform.position = pos;
        }
        else if (_mode == CameraMode.InGame)
        {
            if (_followTarget == null) return;

            var pos = transform.position;
            var targetY = _followTarget.position.y + _verticalOffset;
            if (targetY < _minY) targetY = _minY; // 하한선 아래로는 추적하지 않음
            pos.x = _fixedX;
            float speed = targetY > pos.y ? _riseSpeed : _fallSpeed;
            pos.y = Mathf.Lerp(pos.y, targetY, deltaTime * speed);
            pos += (Vector3)_shaker.LateTick(Time.unscaledDeltaTime);
            transform.position = pos;
        }
    }

    public void Initialize(CameraMode mode)
    {
        _mode = mode;

        _mainCamera = GetComponent<Camera>();
        _shaker = GetComponent<CameraShaker>();
        _baseOrthoSize = _mainCamera.orthographicSize;

        if (_mode == CameraMode.OutGame)
        {
            
        }
        else if (_mode == CameraMode.InGame)
        {
            _fixedX = transform.position.x;
            _verticalOffset = _defaultVerticalOffset;
            _minY = float.NegativeInfinity;
        }
    }

    public void InitializeByMode(Transform target)
    {
        if (_mode == CameraMode.OutGame)
        {
            
        }
        else if (_mode == CameraMode.InGame)
        {
            ActivateFollow(target);
            SnapYToTarget(target);
        }
    }

    // ======== 카메라 크기 ========

    // 기본 줌 대비 배율로 줌. scale < 1 = 줌인, > 1 = 줌아웃.
    // timeScale=0(콤보 러쉬 정지) 중에도 동작하도록 unscaled 업데이트.
    public void ZoomScale(float scale, float duration, Ease ease = Ease.OutQuad)
    {
        _zoomTween?.Kill();
        _zoomTween = _mainCamera.DOOrthoSize(_baseOrthoSize * scale, duration)
            .SetUpdate(true)
            .SetEase(ease)
            .SetLink(gameObject);
    }

    // 기본 줌으로 복귀
    public void ResetZoom(float duration, Ease ease = Ease.OutQuad)
    {
        ZoomScale(1f, duration, ease);
    }

    // ======== 아웃게임 카메라 ========

    public void MoveToTargetPosX(float targetX)
    {
        _fixedX = targetX;
    }

    public void MoveToTargetPosY(float targetY)
    {
        _fixedY = targetY;
    }

    public void SnapToTargetPos(Vector2 target)
    {
        _fixedX = target.x;
        _fixedY = target.y;

        var pos = transform.position;
        pos.x = target.x;
        pos.y = target.y;
        transform.position = pos;
    }

    // ======== 인게임 카메라 ========

    public void ActivateFollow(Transform target)
    {
        _followTarget = target;
    }

    public void DeactivateFollow()
    {
        _followTarget = null;
    }

    public void SnapYToTarget(Transform target)
    {
        var pos = transform.position;
        pos.x = _fixedX;
        pos.y = target.position.y + _verticalOffset;
        transform.position = pos;
    }

    // 카메라가 내려갈 수 있는 월드 Y 하한선을 설정한다 (역주행 방지).
    // 특정 지점 통과 시 호출하면 이후 그 Y 아래로는 카메라가 이동하지 않는다.
    public void SetMinY(float minY)
    {
        _minY = minY;
    }

    // 하한선을 해제하여 자유롭게 따라가도록 되돌린다.
    public void ClearMinY()
    {
        _minY = float.NegativeInfinity;
    }

    public void SetUrgent(bool urgent)
    {
        var target = urgent ? _urgentVerticalOffset : _defaultVerticalOffset;
        var ease = urgent ? _urgentEaseIn : _urgentEaseOut;
        _offsetTween?.Kill();
        _offsetTween = DOTween.To(() => _verticalOffset, v => _verticalOffset = v, target, _urgentDuration)
            .SetEase(ease)
            .SetLink(gameObject);
    }

    // =========== ... ===========

    public void Shake(Vector2 direction, float force) => _shaker?.Shake(direction, force);

    public void BeginConstantShake(float force) => _shaker?.BeginConstantShake(force);
    public void StopConstantShake() => _shaker?.StopConstantShake();
}
