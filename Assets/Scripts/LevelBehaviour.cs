using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class LevelBehaviour : MonoBehaviour
{
    [SerializeField] private MissionConfig _missionConfig;
    [SerializeField] private Transform _startPoint;
    [SerializeField] private Transform _endPoint;
    [SerializeField] private Transform[] _cameraFloorPoints; // 역주행 방지: 플레이어가 지날 때마다 카메라 하한선이 되는 라인들

    private PlayerBehaviour _player;
    public PlayerBehaviour Player => _player;
    private PursuerActor _pursuerActor;
    private PursuerState _pursuerState;
    public PursuerState PursuerState => _pursuerState;
    private EnemyBehaviour[] _enemies;
    private ParallaxBackground _parallax;

    private MissionSystem _missionSystem;
    public MissionSystem MissionSystem => _missionSystem;
    private MissionGenerator _missionGenerator;
    private bool _missionStarted;

    private LevelManager _levelManager;
    private CameraController _cameraController;
    private Action _onFailure;
    private Action _onSuccess;
    private bool _isPlayerReached;

    private Transform[] _sortedFloorPoints; // y 오름차순 정렬된 유효 포인트
    private int _nextFloorIndex;

    private void OnDestroy()
    {
        if (_pursuerState != null)
        {
            if (_pursuerActor != null) _pursuerState.OnTierChanged -= _pursuerActor.MoveToTier;
            _pursuerState.OnCatched -= OnPursuerCatched;
        }
        if (_missionSystem != null)
        {
            if (_player != null)
            {
                _player.OnKilled -= _missionSystem.RecordKill;
                _player.OnLanded -= _missionSystem.ResetProgress;
            }
            if (_player != null) _missionSystem.OnChainThresholdReached -= _player.ActivateComboRush;
            _missionSystem.OnMissionStarted -= RefreshMissionHighlights;
            _missionSystem.OnMissionComplete -= OnMissionComplete;
            _missionSystem.OnMissionFailed -= OnMissionFailed;
        }
    }

    public void Initialize(LevelManager levelManager, CameraController cameraController, IPointerInput pointerInput, Action<int, int> onComboChanged, Action<float, float> onComboRushChanged, Action<int, int> onTierChanged, Action onFailure, Action onSuccess)
    {
        _levelManager = levelManager;
        _cameraController = cameraController;

        _player = GetComponentInChildren<PlayerBehaviour>();
        _player?.Initialize(cameraController, pointerInput, onComboChanged, onComboRushChanged, null, null);
        _pursuerActor = GetComponentInChildren<PursuerActor>();
        _pursuerActor.Initialize(cameraController.MainCamera, cameraController.BaseOrthoSize, _player, PursuerState.START_TIER);
        _pursuerState = new PursuerState();
        _enemies = GetComponentsInChildren<EnemyBehaviour>();
        foreach (var enemy in _enemies)
            enemy?.Initialize(_player, null, null);
        _parallax =GetComponentInChildren<ParallaxBackground>();
        _parallax.Initialize(cameraController.MainCamera.transform, _player.transform);

        _onFailure = onFailure;
        _onSuccess = onSuccess;

        _missionGenerator = new MissionGenerator(_missionConfig);
        _missionSystem = new MissionSystem(_player.SlingBehaviour.Config.comboRushMissionChain);

        _missionSystem.OnMissionFailed += OnMissionFailed;
        _missionSystem.OnMissionComplete += OnMissionComplete;
        _missionSystem.OnMissionStarted += RefreshMissionHighlights;
        _missionSystem.OnChainThresholdReached += _player.ActivateComboRush;
        _player.OnLanded += _missionSystem.ResetProgress;
        _player.OnKilled += _missionSystem.RecordKill;
        _pursuerState.OnCatched += OnPursuerCatched;
        _pursuerState.OnTierChanged += _pursuerActor.MoveToTier;
        _pursuerState.OnTierChanged += onTierChanged;

        _isPlayerReached = false;

        InitCameraFloorPoints();
    }

    public void FixedTick(float fixedDeltaTime)
    {
        _player?.FixedTick(fixedDeltaTime);
        foreach (var enemy in _enemies)
            enemy?.FixedTick(fixedDeltaTime);
    }

    public void Tick(float deltaTime)
    {
        _player?.Tick(deltaTime);
        _pursuerActor?.Tick(deltaTime);
        _parallax?.Tick(deltaTime);

        UpdateCameraFloor();
        CheckFallOffDeath();

        // 플레이 시작(첫 슬링샷으로 Wait 해제) 시점에 첫 미션 시작 (이후 추격자는 미션 성공/실패에 반응)
        if (!_missionStarted && !_levelManager.IsWaiting)
        {
            _missionStarted = true;
            StartNextMission();
        }

        // 미션 타이머는 미션 시작 후, 아직 도착 전, 플레이어 생존 중일 때만 흐른다
        if (_missionStarted && !_isPlayerReached && _player != null && !_player.IsDead && !_player.IsComboRushActive)
        {
            _missionSystem?.Tick(deltaTime);
        }

        foreach (var enemy in _enemies)
            enemy?.Tick(deltaTime);

#if UNITY_EDITOR
        TickCheat();
#endif

        // 엔드포인트 도달 판정 (물리에 쓰지 않고 읽기만 하는 게임 로직 → Tick에 둔다)
        if (!_isPlayerReached
            && _player != null
            && _endPoint != null
            && _player.PlatformerSensor.IsGrounded
            && _player.transform.position.y >= _endPoint.position.y)
        {
            _isPlayerReached = true;
            _onSuccess?.Invoke();
        }
    }

    public void LateTick()
    {
        _pursuerActor?.LateTick();
    }

    // 카메라 하한선 포인트들을 null 제거 후 y 오름차순으로 정렬 (플레이어가 아래에서 위로 지나가는 순서)
    private void InitCameraFloorPoints()
    {
        _nextFloorIndex = 0;
        var valid = new List<Transform>();
        if (_cameraFloorPoints != null)
            foreach (var floorPoint in _cameraFloorPoints)
                if (floorPoint != null) valid.Add(floorPoint);
        valid.Sort((a, b) => a.position.y.CompareTo(b.position.y));
        _sortedFloorPoints = valid.ToArray();
    }

    // 플레이어가 라인을 넘으면 카메라 minY를 "방금 지난 라인의 한 칸 이전 라인"으로 갱신 (역주행 방지).
    // 방금 지난 라인 바로 위에서 막으면 한 발도 못 물러서니, 직전 라인까지는 다시 내려갈 수 있게 한 칸 여유를 둔다.
    // 한 프레임에 여러 라인을 넘어도 while로 가장 높은 라인까지 따라잡는다.
    private void UpdateCameraFloor()
    {
        if (_cameraController == null || _player == null || _sortedFloorPoints == null) return;

        var playerPosY = _player.transform.position.y;

        while (_nextFloorIndex < _sortedFloorPoints.Length
            && playerPosY >= _sortedFloorPoints[_nextFloorIndex].position.y)
        {
            _nextFloorIndex++;
            var floorIndex = _nextFloorIndex - 2; // 방금 지난 라인 = _nextFloorIndex - 1, 그 한 칸 이전 = _nextFloorIndex - 2
            if (floorIndex >= 0) _cameraController.SetMinY(_sortedFloorPoints[floorIndex].position.y);
        }
    }

    // 카메라가 하한에 막혀 플레이어가 화면 아래로 빠져나가면(역주행) 사망 처리.
    // 카메라가 따라 내려가는 동안은 화면 밖으로 나가지 않으므로, 하한에 멈췄을 때만 실제로 발동한다.
    private void CheckFallOffDeath()
    {
        if (_cameraController == null || _player == null || _player.IsDead || _isPlayerReached) return;
        if (_levelManager == null || _levelManager.IsWaiting) return;

        var cameraBottomY = _cameraController.MainCamera.transform.position.y - _cameraController.CameraHeight * 0.5f;
        if (_player.transform.position.y < cameraBottomY)
        {
            _player?.Kill();
            _onFailure?.Invoke();
        }
    }

    // ========== ... ==========

    public void OnStateChanged(InGameState prev, InGameState curr)
    {
        _pursuerActor?.OnStateChanged(prev, curr);
    }

    // ========== 미션 진행 ==========

    // 미션이 바뀔 때마다 살아있는 적들의 강조 외곽선을 갱신한다. 대상 타입만 켜고 나머지는 끈다.
    private void RefreshMissionHighlights(MissionDefinition mission)
    {
        if (_enemies == null || mission == null) return;

        foreach (var enemy in _enemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            enemy.SetMissionHighlight(mission.Requirements.ContainsKey(enemy.EnemyType));
        }
    }

    private void OnMissionFailed()
    {
        _pursuerState?.OnMissionFailure();
        StartNextMission();
    }

    private void OnMissionComplete(float remainingRatio)
    {
        _pursuerState?.OnMissionComplete();
        StartNextMission();
    }

    private void OnPursuerCatched()
    {
        _player?.Kill();
        _onFailure?.Invoke();
    }

    private void StartNextMission()
    {
        if (_missionSystem == null || _missionConfig == null) return;
        if (_player == null || _player.IsDead) return;

        var difficulty = CurrentDifficulty();
        var lookAhead = LookAheadAvailability();
        var mission = _missionGenerator.Generate(difficulty, lookAhead);
        _missionSystem.StartMission(mission);
    }

    // 난이도는 startPoint→endPoint 기준 정규화 — 맵 길이가 바뀌어도 항상 끝에서 maxDifficulty에 도달
    private int CurrentDifficulty()
    {
        if (_startPoint == null || _endPoint == null || _player == null || _missionConfig == null) return 0;
        var totalHeight = Mathf.Max(1f, _endPoint.position.y - _startPoint.position.y);
        var progress = Mathf.Clamp01((_player.Rigid.position.y - _startPoint.position.y) / totalHeight);
        return Mathf.FloorToInt(progress * _missionConfig.maxDifficulty);
    }

    private Dictionary<EnemyType, int> LookAheadAvailability()
    {
        var dictionary = new Dictionary<EnemyType, int>();
        if (_player == null) return dictionary;

        var low = _player.Rigid.position.y;
        var high = low + (_missionConfig != null ? _missionConfig.bandHeight : 20f);

        CountEnemiesInRange(dictionary, low, high);
        if (dictionary.Count == 0) CountEnemiesInRange(dictionary, low, float.PositiveInfinity); // 밴드에 없으면 위쪽 전체로 폴백

        return dictionary;
    }

    private void CountEnemiesInRange(Dictionary<EnemyType, int> dictionary, float low, float high)
    {
        foreach (var enemy in _enemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            var y = enemy.transform.position.y;
            if (y < low || y > high) continue;
            dictionary.TryGetValue(enemy.EnemyType, out var c);
            dictionary[enemy.EnemyType] = c + 1;
        }
    }

#if UNITY_EDITOR
    private void TickCheat()
    {
        var kb = Keyboard.current;
        if (kb == null || _pursuerState == null) return;

        if (kb.digit1Key.wasPressedThisFrame) _pursuerState.CheatSetTier(1);
        else if (kb.digit2Key.wasPressedThisFrame) _pursuerState.CheatSetTier(2);
        else if (kb.digit3Key.wasPressedThisFrame) _pursuerState.CheatSetTier(3);
        else if (kb.digit4Key.wasPressedThisFrame) _pursuerState.CheatSetTier(4);
    }
#endif
}
