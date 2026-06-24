using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class Muffler : MonoBehaviour
{
    [Header("Cloth")]
    [SerializeField] private int _segmentCount = 30;
    [SerializeField] private float _segmentSpacing = 0.3f;
    [SerializeField] private int _solverIter = 5; // 높을수록 뻣뻣함 / 낮을수록 늘어남
    [SerializeField] private float _damping = 0.8f; // 관성 길이 (높을수록 오래 출렁임)
    [SerializeField] private Vector2 _gravity = new(0f, -2f);

    [Header("Wind")]
    [SerializeField] private Vector2 _wind = new(8f, 0f); // 사인파라 +/-로 상쇄되므로 _gravity보다 훨씬 커야 체감됨
    [SerializeField] private float _windFrequency = 1.5f; // 초당 진동 횟수 느낌 (클수록 빠르게 펄럭임)
    [SerializeField] private float _windPhaseStep = 0.6f; // 세그먼트 인덱스(i)마다 위상을 늦춰서 파동처럼 보이게 함

    [Header("Grab")]
    [SerializeField] private float _grabArcHeight = 2f; // 곡선을 그리면서 가져오도록 함 (배지어 곡선의 포물선 높이)

    private LineRenderer _line;
    private Vector2[] _pos, _prevPos;
    private bool _windEnabled;
    private bool _grabbing;
    private float _grabProgress;
    private Vector2 _grabTargetPos;
    private Coroutine _grabRoutine;

    private void LateUpdate()
    {
        if (_grabbing)
            LayAlongBezier();
        else
            SimulateVerlet();

        for (int i = 0; i < _segmentCount; i++)
            _line.SetPosition(i, _pos[i]);
    }

    public void Initialize()
    {
        _line = GetComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.positionCount = _segmentCount;

        _pos = new Vector2[_segmentCount];
        _prevPos = new Vector2[_segmentCount];
        var start = (Vector2)transform.position;
        for (int i = 0; i < _segmentCount; i++)
            _pos[i] = _prevPos[i] = start + Vector2.down * (_segmentSpacing * i);
        _windEnabled = true;
    }

    /// <summary>
    /// 베를렛 알고리즘(Verlet Integration)을 이용해 머플러 물리 구현
    /// </summary>
    private void SimulateVerlet()
    {
        var dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 60f); // 스파이크 방어
        var dt2 = dt * dt;
        var baseForce = _gravity * dt2;
        var t = Time.unscaledTime;

        // 1. 적분 — (pos - prevPos)가 관성(속도) 역할
        for (int i = 1; i < _segmentCount; i++)
        {
            // i마다 위상이 다른 사인파 -> 세그먼트별로 다른 시점에 좌우로 밀려서 파동처럼 보임
            // sin 값은 -1~1로 계속 부호가 바뀌므로(상쇄됨) _gravity처럼 한쪽으로 누적되지 않음 -> amplitude를 크게 잡아야 체감됨
            var windForce = _windEnabled
                ? _wind * (Mathf.Sin(t * _windFrequency + i * _windPhaseStep) * dt2)
                : Vector2.zero;
            var vel = (_pos[i] - _prevPos[i]) * _damping;
            _prevPos[i] = _pos[i];
            _pos[i] += vel + baseForce + windForce;
        }

        // 2. 앵커 고정
        _pos[0] = transform.position;

        // 3. 길이 제약 반복
        // 한쪽 방향(0->끝)으로만 풀면 보정이 누적되어 끝(자유단)만 많이 움직이므로,
        // 매 iter마다 정방향+역방향을 한 쌍으로 묶어 항상 대칭으로 풀어준다
        // (정/역 횟수가 어긋나면 한쪽으로 치우친 잔여 보정이 누적되어 매 프레임 같은 방향으로 쏠리는 버그가 생김)
        for (int k = 0; k < _solverIter; k++)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                var forward = (pass == 0);
                var start = forward ? 0 : _segmentCount - 2;
                var end = forward ? _segmentCount - 1 : -1;
                var step = forward ? 1 : -1;

                for (int i = start; i != end; i += step)
                {
                    var delta = _pos[i + 1] - _pos[i];
                    var dist = delta.magnitude;
                    if (dist < 1e-5f) continue;
                    var diff = (dist - _segmentSpacing) / dist;
                    if (i != 0) _pos[i] += delta * (0.5f * diff);
                    _pos[i + 1]         -= delta * (0.5f * diff);
                }
            }
        }
    }

    public void SetFacingDir(bool facingRight)
    {
        _gravity.x = Mathf.Abs(_gravity.x) * (facingRight ? -1f : 1f);
        _wind.x = Mathf.Abs(_wind.x) * (facingRight ? 1f : -1f);
    }

    public void SetWindEnabled(bool enabled)
    {
        _windEnabled = enabled;
    }

    public void PlayGrabAnimation(Vector2 targetPos, float duration, System.Action onComplete = null)
    {
        if (_grabRoutine != null) StopCoroutine(_grabRoutine);
        _grabRoutine = StartCoroutine(GrabAimationRoutine(targetPos, duration, onComplete));
    }

    // 앵커->타겟 베지어 위에 모든 세그먼트를 분배
    private void LayAlongBezier()
    {
        Vector2 a = transform.position;
        Vector2 b = _grabTargetPos;
        Vector2 dir = b - a;
        Vector2 perp = new Vector2(-dir.y, dir.x).normalized; // 진행 방향 기준 왼쪽 수직
        Vector2 ctrl = (a + b) * 0.5f + perp * _grabArcHeight;

        int last = _segmentCount - 1;
        for (int i = 0; i < _segmentCount; i++)
        {
            float t = (i / (float)last) * _grabProgress;
            _pos[i] = QuadraticBezier(a, ctrl, b, t);
            _prevPos[i] = _pos[i]; // 그랩 해제 시 관성 튐 방지
        }
    }

    private IEnumerator GrabAimationRoutine(Vector2 targetPos, float duration, System.Action onComplete)
    {
        _grabTargetPos = targetPos;
        _grabbing = true;
        float half = duration * 0.5f;

        yield return Tween(0f, 1f, half); // 뻗기
        yield return Tween(1f, 0f, half); // 복귀

        _grabbing = false;
        onComplete?.Invoke();
    }

    private IEnumerator Tween(float from, float to, float dur)
    {
        float e = 0f;
        while (e < dur)
        {
            _grabProgress = Mathf.Lerp(from, to, e / dur);
            e += Time.unscaledDeltaTime;
            yield return null;
        }
        _grabProgress = to;
    }

    private static Vector2 QuadraticBezier(Vector2 a, Vector2 c, Vector2 b, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * c + t * t * b;
    }
}
