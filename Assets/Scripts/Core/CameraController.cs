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
    private float _followY; // 셰이크가 섞이지 않은 인게임 카메라 베이스 Y (셰이크 누적 방지)
    private Transform _followTarget;
    private Tween _zoomTween;
    private Tween _offsetTween;
    private Tween _followYTween;
    private float _minY = float.NegativeInfinity; // 카메라가 이 월드 Y 아래로 내려가지 못하도록 막는 하한선 (역주행 방지)

    // =========== ... ===========

    private float _fixedX;
    private float _fixedY;
    private float _baseOrthoSize;
    private CameraShaker _shaker;

    public float BaseOrthoSize => _baseOrthoSize; // 줌과 무관한 기준 크기 (줌 영향 받지 않을 요소용)

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

            var targetY = _followTarget.position.y + _verticalOffset;
            if (targetY < _minY) targetY = _minY; // 하한선 아래로는 추적하지 않음

            // 평소엔 _fixedX 고정(성벽 폭이 기본 ortho에 딱 맞음). 줌인 시 가시 폭이 좁아져 생긴 여유만큼만
            // 좌우로 움직여 플레이어를 프레임에 잡되, 성벽 밖이 보이지 않도록 클램프한다.
            // 줌이 기본 배율로 복귀하면 이동 가능 폭이 0이 되어 자동으로 _fixedX로 되돌아온다.
            var maxHorizontalPan = (_baseOrthoSize - _mainCamera.orthographicSize) * CameraAspect;
            var leftBound = _fixedX - maxHorizontalPan;
            var rightBound = _fixedX + maxHorizontalPan;
            var baseX = maxHorizontalPan > 0f
                ? Mathf.Clamp(_followTarget.position.x, leftBound, rightBound)
                : _fixedX;

            // 베이스 Y는 셰이크가 섞이지 않은 _followY에서만 누적한다. 셰이크는 마지막에 순수 오프셋으로 얹어
            // timeScale=0(줌 정지) 구간에서도 셰이크가 위치에 쌓여 드리프트하지 않도록 한다.
            float speed = targetY > _followY ? _riseSpeed : _fallSpeed;
            _followY = Mathf.Lerp(_followY, targetY, deltaTime * speed);

            var shake = (Vector3)_shaker.LateTick(Time.unscaledDeltaTime);
            transform.position = new Vector3(baseX, _followY, transform.position.z) + shake;
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

    // 콤보 러쉬 줌인 등에서 플레이어를 화면 세로 정중앙에 맞춘다.
    // 가로(X)는 LateTick의 pan 로직이 줌인 배율에 맞춰 자동으로 플레이어를 향해 좁혀가며 (벽 안에서) 중앙에 맞춘다.
    // 세로(Y)는 정지(timeScale=0) 중 LateTick의 lerp가 멈춰(_followY 고정) 갱신되지 않으므로,
    // 줌인과 동일한 시간 동안 unscaled 트윈으로 베이스 Y를 플레이어 Y(오프셋 0 = 정중앙)까지 직접 내린다.
    // 연출 종료 후 timeScale이 복귀하면 LateTick이 _verticalOffset 기준 기본 구도로 자연스럽게 되돌린다.
    public void CenterVerticalOnTarget(Transform target, float duration, Ease ease = Ease.OutQuad)
    {
        if (target == null) return;
        _followYTween?.Kill();
        _followYTween = DOTween.To(() => _followY, v => _followY = v, target.position.y, duration)
            .SetUpdate(true)
            .SetEase(ease)
            .SetLink(gameObject);
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
        _followY = pos.y; // 베이스 Y 동기화 (LateTick이 이 값에서 lerp)
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

    // 방향 없이 무작위 방향으로 흔든다 — "그냥 화면 흔들림"용
    public void Shake(float force = 0.3f) => _shaker?.Shake(force);

    public void BeginConstantShake(float force) => _shaker?.BeginConstantShake(force);
    public void StopConstantShake() => _shaker?.StopConstantShake();
}
