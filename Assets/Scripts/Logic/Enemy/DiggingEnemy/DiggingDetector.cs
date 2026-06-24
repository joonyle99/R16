using UnityEngine;

public class DiggingDetector : MonoBehaviour
{
    [SerializeField] private float _detectionWidth  = 1f;
    [SerializeField] private float _detectionHeight = 5f;
    [SerializeField] private float _warningDuration = 1.2f;
    [SerializeField] private LayerMask _playerLayer;
    
    public float WarningDuration => _warningDuration;

    public RaycastHit2D DetectTarget()
    {
        var hit = Physics2D.BoxCast(
            transform.position,
            new Vector2(_detectionWidth, 0.1f),
            0f,
            Vector2.up,
            _detectionHeight,
            _playerLayer
        );

        return hit;
    }

    public void ShowWarning()
    {
        
    }

    public void HideWarning()
    {
        
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            transform.position + Vector3.up * (_detectionHeight * 0.5f),
            new Vector3(_detectionWidth, _detectionHeight, 0f)
        );
    }
#endif
}
