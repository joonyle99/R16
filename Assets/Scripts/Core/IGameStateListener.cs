using System;

public interface IGameStateListener<T> where T : Enum
{
    void OnStateChanged(T prevState, T currState);
}
