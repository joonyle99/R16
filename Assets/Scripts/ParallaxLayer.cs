using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float _parallaxFactor = 0.5f; // 0 = 고정, 1 = 카메라와 동일 속도

    private Transform _cameraTrans;
    private float _lastCameraPosY;
    private bool _isActive = false;

    public void Initialize(Transform cameraTrans)
    {
        _cameraTrans = cameraTrans;
    }

    public void Tick()
    {
        if (!_isActive) return;

        float currCameraPosY = _cameraTrans.position.y;
        float deltaPosY = currCameraPosY - _lastCameraPosY;

        transform.position += new Vector3(0f, deltaPosY * _parallaxFactor, 0f);

        _lastCameraPosY = currCameraPosY;
    }

    public void Activate()
    {
        _isActive = true;
        _lastCameraPosY = _cameraTrans.position.y;
    }

    public void DeActivate()
    {
        _isActive = false;
    }
}
