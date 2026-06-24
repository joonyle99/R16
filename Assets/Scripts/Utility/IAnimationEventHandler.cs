/// <summary>
/// 애니메이션 이벤트를 받아서 처리하는 인터페이스
/// </summary>
public interface IAnimationEventHandler
{
    /// <summary>
    /// 애니메이션 이벤트에서 호출되는 함수
    /// </summary>
    void OnAnimationEvent(string eventName);
}
