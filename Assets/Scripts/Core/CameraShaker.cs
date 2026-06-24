using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [SerializeField] private float _decay = 15f;      // 셰이크 감쇠 속도 — 클수록 빠르게 잦아듦
    [SerializeField] private float _frequency = 30f;  // 진동 빠르기 (Hz) — 클수록 잘게 떨림
    [SerializeField] private float _perpRatio = 0.6f; // 단발 킥의 수직 성분 비율 — 방향축에 수직으로 섞어 한 축(특히 세로)만 묻히는 현상 방지

    [Header("Test")]
    [SerializeField] private float _testMagnitude = 0.3f;
    [SerializeField] private Vector2 _testDirection = Vector2.right;

    private Vector2 _dir;
    private float _magnitude;        // 단발성 — 시간에 따라 감쇠
    private float _shakeElapsed;     // 단발성 전용 경과 시간 — 호출마다 0에서 시작해 위상 고정 (전역 시간에 묶이지 않도록)
    private float _constMagnitude;   // 상시 — 명시적으로 멈출 때까지 유지

    public Vector2 LateTick(float deltaTime)
    {
        var offset = Vector2.zero;

        // 단발성 셰이크: 방향축 + 수직축으로 작은 타원형 킥을 그리며 감쇠
        if (_magnitude > 0f)
        {
            _shakeElapsed += deltaTime;
            _magnitude = Mathf.MoveTowards(_magnitude, 0f, _decay * deltaTime);

            var perp = new Vector2(-_dir.y, _dir.x); // _dir을 90° 회전한 수직축
            var phase = _shakeElapsed * _frequency;
            // 주축: cos(정점에서 시작)로 첫 프레임부터 _dir 방향 꽉 찬 킥. 수직축: 90° 늦은 sin을 작게 섞어
            // 어느 방향 히트든 양축에 모션을 만든다(세로 킥이 정지된 가로축을 때려 묻히지 않도록). 매 호출 동일 곡선.
            offset += _dir * (_magnitude * Mathf.Cos(phase))
                    + perp * (_magnitude * _perpRatio * Mathf.Sin(phase));
        }

        // 상시 셰이크: 펄린 노이즈로 양축을 흔들어 지속적인 럼블
        if (_constMagnitude > 0f)
        {
            var t = Time.unscaledTime * _frequency;
            var noise = new Vector2(Mathf.PerlinNoise(t, 0f) - 0.5f, Mathf.PerlinNoise(0f, t) - 0.5f);
            offset += noise * (2f * _constMagnitude);
        }

        return offset;
    }

    public void Shake(Vector2 direction, float magnitude)
    {
        _dir = direction.normalized;
        _magnitude = magnitude;
        _shakeElapsed = 0f; // 호출마다 위상 리셋 → 항상 정점(cos)에서 시작하는 동일 곡선
    }

    // 방향 없이 무작위 방향으로 흔든다 — "그냥 화면 흔들림"용
    public void Shake(float magnitude) => Shake(Random.insideUnitCircle.normalized, magnitude);

    public void BeginConstantShake(float magnitude) => _constMagnitude = magnitude;
    public void StopConstantShake() => _constMagnitude = 0f;

    public void TestShake() => Shake(_testDirection, _testMagnitude);
}
