using UnityEngine;

public class CloudScroller : MonoBehaviour
{
    [SerializeField] private float _scrollSpeed = 0.5f;
    [SerializeField] private float _resetPosX;
    [SerializeField] private float _startPosX;

    private Vector3 _initialPosition;

    private void Awake()
    {
        _initialPosition = transform.position;
    }

    public void ResetPosition()
    {
        transform.position = _initialPosition;
    }

    private void Update()
    {
        transform.Translate(Vector3.left * _scrollSpeed * Time.deltaTime);

        if (transform.position.x <= _resetPosX)
        {
            var pos = transform.position;
            pos.x = _startPosX;
            transform.position = pos;
        }
    }

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool _drawGizmos = true;
    [SerializeField] private float _gizmoLineHeight = 5f;

    private void OnDrawGizmos()
    {
        if (!_drawGizmos)
        {
            return;
        }

        var y = transform.position.y;
        var z = transform.position.z;
        var halfHeight = _gizmoLineHeight * 0.5f;

        // 리셋 지점(왼쪽 한계) — 빨강
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(_resetPosX, y - halfHeight, z), new Vector3(_resetPosX, y + halfHeight, z));

        // 시작 지점(되돌아갈 위치) — 초록
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(_startPosX, y - halfHeight, z), new Vector3(_startPosX, y + halfHeight, z));

        // 스크롤 방향(왼쪽) — 노랑
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * _scrollSpeed);

        // 현재 위치 — 흰색 구
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, 0.2f);

        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.Label(new Vector3(_resetPosX, y + halfHeight, z), "Reset");
        UnityEditor.Handles.color = Color.green;
        UnityEditor.Handles.Label(new Vector3(_startPosX, y + halfHeight, z), "Start");
    }
#endif
}
