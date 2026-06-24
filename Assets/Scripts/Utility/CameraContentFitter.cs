using UnityEngine;

/// <summary>
/// 직교 카메라의 사이즈를 콘텐츠의 가로 길이에 딱 맞도록 한다
/// 어떤 해상도이든 콘텐츠가 모두 보여야 할 때 사용한다
/// </summary>
[RequireComponent(typeof(CameraController))]
public class CameraContentFitter : MonoBehaviour
{
    [SerializeField] private Renderer _content;

    public Vector3 ContentSize => _content.bounds.size;
    public float ContentWidth => _content.bounds.size.x;
    public float ContentHeight => _content.bounds.size.y;
    public Vector3 ContentMinPoint => _content.bounds.min;
    public Vector3 ContentMaxPoint => _content.bounds.max;

    [SerializeField] private float _minOrthograpicSize = 3f;
    public float MinOrthograpicSize => _minOrthograpicSize;
    private float _maxOrthograpicSize;
    public float MaxOrthograpicSize => _maxOrthograpicSize;

    private CameraController _cameraController;

    private int _lastScreenWidth;
    private int _lastScreenHeight;

    public void Initialize(CameraController cameraController)
    {
        _cameraController = cameraController;

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        // UpdateOrthograpicSize();
    }

    /// <summary>
    /// 콘텐츠를 화면에 모두 담는다
    /// </summary>
    /// <remarks>
    /// 1. 화면 높이 = OrthograpicSize * 2
    /// 2. 화면 너비 = 화면 높이 * Aspect
    /// </remarks>
    private void UpdateOrthograpicSize()
    {
        // var orthograpicSize = ContentWidth / _cameraController.CameraAspect * 0.5f;
        // _cameraController.SetOrthographicSize(orthograpicSize);
        // _maxOrthograpicSize = orthograpicSize;
    }
}