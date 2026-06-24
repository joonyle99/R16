using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    private enum Axis { X, Y } // 한 레이어는 한 축만 따라간다 (X 또는 Y)

    [SerializeField] private Axis _axis = Axis.Y;
    [SerializeField, Range(0f, 1f)] private float _parallaxFactor = 0.5f; // 0 = 고정, 1 = 카메라와 동일 속도

    private Transform _cameraTrans;
    private float _lastCameraPos;
    private bool _isActive = false;

    public void Initialize(Transform cameraTrans)
    {
        _cameraTrans = cameraTrans;
    }

    public void Tick()
    {
        if (!_isActive) return;

        float currCameraPos = CameraAxisPos();
        float deltaPos = currCameraPos - _lastCameraPos;

        var move = _axis == Axis.X
            ? new Vector3(deltaPos * _parallaxFactor, 0f, 0f)
            : new Vector3(0f, deltaPos * _parallaxFactor, 0f);
        transform.position += move;

        _lastCameraPos = currCameraPos;
    }

    public void Activate()
    {
        _isActive = true;
        _lastCameraPos = CameraAxisPos();
    }

    public void DeActivate()
    {
        _isActive = false;
    }

    private float CameraAxisPos()
        => _axis == Axis.X ? _cameraTrans.position.x : _cameraTrans.position.y;
}
