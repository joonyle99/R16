using UnityEngine;

public class PursuerEyeTracker : MonoBehaviour
{
    [SerializeField] private Transform[] _eyes;
    [SerializeField] private float _detectionRange = 4.5f;
    [SerializeField] private float _eyeMaxOffset = 0.3f;
    [SerializeField] private float _speed = 12f;

    [SerializeField] private float _crazyMaxOffsetX = 0.4f;
    [SerializeField] private float _crazyMaxOffsetY = 0.3f;
    [SerializeField] private float _crazySpeed = 3f;

    private Rigidbody2D _target;
    private float[] _centerLocalXs;
    private float[] _centerLocalYs;
    private float _lastFlipSign;
    private bool _isCrazy;
    private float _crazyTime;

    public void Initialize(Rigidbody2D target)
    {
        _target = target;
        _lastFlipSign = _eyes.Length > 0 ? Mathf.Sign(_eyes[0].lossyScale.x) : 1f;

        _centerLocalXs = new float[_eyes.Length];
        _centerLocalYs = new float[_eyes.Length];
        for (int i = 0; i < _eyes.Length; i++)
        {
            _centerLocalXs[i] = _eyes[i].localPosition.x;
            _centerLocalYs[i] = _eyes[i].localPosition.y;
        }
    }

    public void SetCrazy(bool crazy) => _isCrazy = crazy;

    public void Tick(float deltaTime)
    {
        if (_target == null) return;

        if (_isCrazy)
        {
            _crazyTime += deltaTime * _crazySpeed;
            
            for (int i = 0; i < _eyes.Length; i++)
            {
                var noiseX = Mathf.PerlinNoise(_crazyTime + i * 13.7f, i * 7.3f) * 2f - 1f;
                var noiseY = Mathf.PerlinNoise(_crazyTime + i * 5.1f + 100f, i * 3.9f + 50f) * 2f - 1f;
                var local = _eyes[i].localPosition;
                local.x = Mathf.Lerp(local.x, _centerLocalXs[i] + noiseX * _crazyMaxOffsetX, _speed * deltaTime);
                local.y = Mathf.Lerp(local.y, _centerLocalYs[i] + noiseY * _crazyMaxOffsetY, _speed * deltaTime);
                _eyes[i].localPosition = local;
            }

            return;
        }

        var flipSign = _eyes.Length > 0 ? Mathf.Sign(_eyes[0].lossyScale.x) : 1f;
        var flipped = flipSign != _lastFlipSign;
        if (flipped)
        {
            for (int i = 0; i < _eyes.Length; i++)
            {
                var pos = _eyes[i].localPosition;
                pos.x = -pos.x;
                _eyes[i].localPosition = pos;
                _centerLocalXs[i] = -_centerLocalXs[i];
            }
            _lastFlipSign = flipSign;
        }

        var distSqr = ((Vector2)_target.position - (Vector2)transform.position).sqrMagnitude;
        var inRange = distSqr <= _detectionRange * _detectionRange;
        var offsetX = _target.position.x - transform.position.x;

        for (int i = 0; i < _eyes.Length; i++)
        {
            var targetLocalX = inRange
                ? _centerLocalXs[i] + Mathf.Clamp(offsetX * flipSign, -_eyeMaxOffset, _eyeMaxOffset)
                : _centerLocalXs[i];

            var local = _eyes[i].localPosition;
            local.x = Mathf.Lerp(local.x, targetLocalX, _speed * deltaTime);
            local.y = Mathf.Lerp(local.y, _centerLocalYs[i], _speed * deltaTime);
            _eyes[i].localPosition = local;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);

        if (_eyes == null || _eyes.Length == 0) return;

        var flipSign = Mathf.Sign(_eyes[0].lossyScale.x);

        foreach (var eye in _eyes)
        {
            if (eye == null) continue;

            var center = eye.parent != null ? eye.parent.position : transform.position;

            Gizmos.color = Color.cyan;
            var left  = center + Vector3.right * flipSign * -_eyeMaxOffset;
            var right = center + Vector3.right * flipSign *  _eyeMaxOffset;
            Gizmos.DrawLine(left, right);
            Gizmos.DrawWireSphere(left,  0.03f);
            Gizmos.DrawWireSphere(right, 0.03f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(eye.position, new Vector3(_crazyMaxOffsetX * 2f, _crazyMaxOffsetY * 2f, 0f));

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(eye.position, 0.04f);
        }

        if (_target == null) return;

        var distSqr = ((Vector2)_target.position - (Vector2)transform.position).sqrMagnitude;
        if (distSqr > _detectionRange * _detectionRange) return;

        Gizmos.color = Color.green;
        foreach (var eye in _eyes)
        {
            if (eye != null)
                Gizmos.DrawLine(eye.position, _target.position);
        }
    }
#endif
}
