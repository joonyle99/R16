using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [SerializeField] private float _decay = 15f;      // 셰이크 감쇠 속도 — 클수록 빠르게 잦아듦
    [SerializeField] private float _frequency = 30f;  // 진동 빠르기 (Hz) — 클수록 잘게 떨림

    [Header("Test")]
    [SerializeField] private float _testMagnitude = 0.3f;
    [SerializeField] private Vector2 _testDirection = Vector2.right;

    private Vector2 _dir;
    private float _magnitude;        // 단발성 — 시간에 따라 감쇠
    private float _constMagnitude;   // 상시 — 명시적으로 멈출 때까지 유지

    public Vector2 LateTick(float deltaTime)
    {
        var offset = Vector2.zero;

        // 단발성 셰이크: 방향축을 따라 진동하며 감쇠
        if (_magnitude > 0f)
        {
            _magnitude = Mathf.MoveTowards(_magnitude, 0f, _decay * deltaTime);
            offset += _dir * (_magnitude * Mathf.Sin(Time.unscaledTime * _frequency));
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
    }

    // 방향 없이 무작위 방향으로 흔든다 — "그냥 화면 흔들림"용
    public void Shake(float magnitude) => Shake(Random.insideUnitCircle.normalized, magnitude);

    public void BeginConstantShake(float magnitude) => _constMagnitude = magnitude;
    public void StopConstantShake() => _constMagnitude = 0f;

    public void TestShake() => Shake(_testDirection, _testMagnitude);
}
