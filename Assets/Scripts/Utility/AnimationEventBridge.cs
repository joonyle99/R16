using UnityEngine;

/// <summary>
/// Animator가 자식 오브젝트로 분리된 경우 부모의 이벤트 핸들러로 애니메이션 이벤트를 전달
/// </summary>
public class AnimationEventBridge : MonoBehaviour
{
    private IAnimationEventHandler _handler;

    private void Awake()
    {
        _handler = GetComponentInParent<IAnimationEventHandler>();
    }

    /// <summary>
    /// 애니메이션 이벤트에서 Function: OnEvent, String: "Attack" 형태로 호출
    /// </summary>
    public void OnEvent(string eventName)
    {
        _handler?.OnAnimationEvent(eventName);
    }
}
