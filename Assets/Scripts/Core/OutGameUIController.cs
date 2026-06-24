using UnityEngine;

public class OutGameUIController : MonoBehaviour, IGameStateListener<OutGameState>
{
    private void OnDestroy()
    {

    }

    public void Initialize()
    {
        
    }

    public void OnStateChanged(OutGameState prevState, OutGameState currState)
    {
        
    }
}
