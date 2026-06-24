using System;
using UnityEngine;

public enum OutGameState
{
    None,
    Room_Store,
    Room_Lobby,
    Room_Practice,
}

public enum InGameState
{
    None,
    Wait,
    Play,
    Failure,
    Success,
}

public class GameStateController<T> where T : Enum
{
    private T currState;
    public T CurrState => currState;

    public event Action<T, T> OnStateChanged;

    public void ChangeState(T nextState)
    {
        if (currState.Equals(nextState)) return;

        var prevState = currState;
        ExitState(currState);
        currState = nextState;
        EnterState(currState);

        OnStateChanged?.Invoke(prevState, currState);
    }

    private void EnterState(T state)
    {
#if UNITY_EDITOR
        Debug.Log($"<color=yellow>Enter State - {state.ToString()}</color>");
#endif
    }

    private void ExitState(T state)
    {
#if UNITY_EDITOR
        Debug.Log($"<color=yellow>Exit State - {state.ToString()}</color>");
#endif
    }
}
