using System;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    private CameraController _cameraController;
    private IPointerInput _pointerInput;
    private Action _onFailure;
    private Action _onSuccess;
    private Action<int, int> _onComboChanged;
    private Action<float, float> _onComboRushChanged;
    private Action<int, int> _onTierChanged;

    [SerializeField] private LevelData[] _levels;

    private int _index;
    private LevelBehaviour _currLv;
    public PlayerBehaviour Player => _currLv?.Player;
    public PursuerState PursuerState => _currLv?.PursuerState;
    public MissionSystem MissionSystem => _currLv?.MissionSystem;
    private bool _isWaiting;
    public bool IsWaiting => _isWaiting;

    public void Initialize(CameraController cameraController, IPointerInput pointerInput, Action<int, int> onComboChanged, Action<float, float> onComboRushChanged, Action<int, int> onTierChanged, Action onFailure, Action onSuccess)
    {
        _cameraController = cameraController;
        _pointerInput = pointerInput;
        _onComboChanged = onComboChanged;
        _onComboRushChanged = onComboRushChanged;
        _onTierChanged = onTierChanged;
        _onFailure = onFailure;
        _onSuccess = onSuccess;

        LoadNext();
    }

    public void FixedTick(float fixedDeltaTime)
    {
        _currLv?.FixedTick(fixedDeltaTime);
    }

    public void Tick(float deltaTime)
    {
        _currLv?.Tick(deltaTime);
    }
    
    public void LateTick()
    {
        _currLv?.LateTick();
    }

    public void LoadNext()
    {
        if (_levels == null || _levels.Length == 0) return;
        if (_currLv != null) Destroy(_currLv.gameObject);
        var levelData = _levels[_index % _levels.Length];
        if (levelData == null) return;

        _currLv = Instantiate(levelData.levelPrefab, transform);
        _currLv.Initialize(this, _cameraController, _pointerInput, _onComboChanged, _onComboRushChanged, _onTierChanged, _onFailure, _onSuccess);

        _index++;
    }

    public void OnStateChanged(InGameState prevState, InGameState currState)
    {
        _isWaiting = currState == InGameState.Wait;
        _currLv?.OnStateChanged(prevState, currState);
    }
}
