using UnityEngine;

public class EyeTracker : MonoBehaviour
{
    [SerializeField] private Transform _eye;
    [SerializeField] private PolygonCollider2D _boundary;
    [SerializeField] private float _range = 4.5f;
    [SerializeField] private float _speed = 12f;

    private Rigidbody2D _target;
    private Rigidbody2D _rigid;
    private float _lastFlipSign;

    public void Initialize(Rigidbody2D target)
    {
        _target = target;
        _rigid = GetComponentInChildren<Rigidbody2D>();
        _lastFlipSign = Mathf.Sign(_eye.lossyScale.x);
    }

    public void Tick(float deltaTime)
    {
        if (_target == null || _boundary == null) return;

        var flipSign = Mathf.Sign(_eye.lossyScale.x);
        if (flipSign != _lastFlipSign)
        {
            var pos = _eye.localPosition;
            pos.x = -pos.x;
            _eye.localPosition = pos;
            _lastFlipSign = flipSign;
        }

        var boundaryCenter = GetBoundaryCenter();
        var distVector = _target.position - (Vector2)transform.position;
        var direction = ((Vector2)_target.position - boundaryCenter).normalized;
        var boundaryPoint = GetBoundaryExitPoint(boundaryCenter, direction);

        var isTargetInRange = distVector.sqrMagnitude <= _range * _range;
        var targetWorldPos = isTargetInRange ? boundaryPoint : boundaryCenter;

        _eye.position = Vector3.Lerp(_eye.position, targetWorldPos, _speed * deltaTime);
    }

    // boundary 폴리곤 꼭짓점들의 Transform 기준 평균 중심
    private Vector2 GetBoundaryCenter()
    {
        var pts = _boundary.points;
        var sum = Vector2.zero;
        for (int i = 0; i < pts.Length; i++)
            sum += (Vector2)_boundary.transform.TransformPoint(pts[i]);
        return sum / pts.Length;
    }

    // Physics2D 없이 Transform 기준으로 ray-polygon 교차점 계산
    private Vector2 GetBoundaryExitPoint(Vector2 origin, Vector2 direction)
    {
        var pts = _boundary.points;
        var bestT = float.MaxValue;
        for (int i = 0; i < pts.Length; i++)
        {
            var a = (Vector2)_boundary.transform.TransformPoint(pts[i]);
            var b = (Vector2)_boundary.transform.TransformPoint(pts[(i + 1) % pts.Length]);
            if (TryRaySegmentIntersect(origin, direction, a, b, out float t) && t > 0f && t < bestT)
                bestT = t;
        }
        return bestT < float.MaxValue ? origin + direction * bestT : origin;
    }

    private bool TryRaySegmentIntersect(Vector2 ro, Vector2 rd, Vector2 a, Vector2 b, out float t)
    {
        var ab = b - a;
        var denom = rd.x * ab.y - rd.y * ab.x;
        t = 0f;
        if (Mathf.Abs(denom) < 1e-6f) return false;
        var ao = a - ro;
        t = (ao.x * ab.y - ao.y * ab.x) / denom;
        var u = (ao.x * rd.y - ao.y * rd.x) / denom;
        return u >= 0f && u <= 1f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _range);

        if (_boundary == null) return;

        // boundary 외곽선 (cyan)
        Gizmos.color = Color.cyan;
        var points = _boundary.points;
        for (int i = 0; i < points.Length; i++)
        {
            var a = _boundary.transform.TransformPoint(points[i]);
            var b = _boundary.transform.TransformPoint(points[(i + 1) % points.Length]);
            Gizmos.DrawLine(a, b);
        }

        var boundaryCenter = GetBoundaryCenter();

        // boundary center (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(boundaryCenter, 0.04f);

        if (_target == null) return;

        var distVector = _target.position - (_rigid != null ? (Vector2)transform.position : (Vector2)transform.position);

        // 타겟 방향 (초록 - 범위 내일 때만)
        if (distVector.sqrMagnitude <= _range * _range)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(boundaryCenter, (Vector2)_target.position);
        }

        if (distVector.sqrMagnitude == 0f) return;

        // 교차점 (빨간색)
        var direction = ((Vector2)_target.position - boundaryCenter).normalized;
        var exitPoint = GetBoundaryExitPoint(boundaryCenter, direction);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(exitPoint, 0.04f);
        Gizmos.DrawLine(boundaryCenter, exitPoint);
    }
#endif
}
