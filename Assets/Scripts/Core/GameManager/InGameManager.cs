using System;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class InGameManager : MonoBehaviour
{
    private PointerInput _pointerInput;
    private GameStateController<InGameState> _inGameStateController;
    private CameraController _cameraController;
    private InGameUIController _inGameUIController;
    private LevelManager _levelManager;
    private MissionSystem _missionSystem;
    private IrisMaskTransition _irisMaskTransition;

    private float _timer;
    private bool _isTimerRunning;

    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (_missionSystem != null)
        {
            var missionPanel = _inGameUIController.MissionPanel;
            _missionSystem.OnProgressReset -= missionPanel.ResetProgress;
            _missionSystem.OnTimerChanged -= missionPanel.SetTimer;
            _missionSystem.OnProgressChanged -= missionPanel.SetProgress;
            _missionSystem.OnMissionStarted -= missionPanel.SetMission;
        }
        if (_levelManager?.Player != null)
        {
            _missionSystem.OnChainChanged -= _inGameUIController.MissionPanel.SetChainProgress;
            _levelManager.Player.SlingBehaviour.OnShootSling -= OnFirstSlingshot;
        }
        _pointerInput.OnTap -= OnFirstPress;
        _inGameStateController.OnStateChanged -= OnStateChanged;
        _inGameStateController.OnStateChanged -= _levelManager.OnStateChanged;
        _inGameStateController.OnStateChanged -= _inGameUIController.OnStateChanged;
        if (SoundManager.Instance != null) _inGameStateController.OnStateChanged -= SoundManager.Instance.OnStateChanged;

        _pointerInput.Dispose();
    }

    private void FixedUpdate()
    {
        var fixedDeltaTime = Time.fixedDeltaTime;
        _levelManager?.FixedTick(fixedDeltaTime);
    }

    private void Update()
    {
        var deltaTime = Time.deltaTime;
        if (_isTimerRunning) _timer += Time.unscaledDeltaTime; // 슬링샷 조준 중 슬로우를 고려하지 않는다
        _pointerInput?.Tick(deltaTime);
        _levelManager?.Tick(deltaTime);
    }

    private void LateUpdate()
    {
        var deltaTime = Time.deltaTime;
        _cameraController?.LateTick(deltaTime);
        _levelManager?.LateTick();
    }

    private void Initialize()
    {
        _inGameStateController = new GameStateController<InGameState>();
        _cameraController = FindFirstObjectByType<CameraController>();
        _inGameUIController = FindFirstObjectByType<InGameUIController>();
        _levelManager = FindFirstObjectByType<LevelManager>();
        _irisMaskTransition = FindFirstObjectByType<IrisMaskTransition>();
        _cameraController.Initialize(CameraMode.InGame);
        _pointerInput = new PointerInput(_cameraController.MainCamera);
        _inGameUIController.Initialize();
        _levelManager.Initialize(
            _cameraController,
            _pointerInput,
            (currCombo, maxCombo) =>
            {
                // _inGameUIController.ComboPanel.SetVisible(currCombo > 0);
                _inGameUIController.ComboPanel.SetComboGague(currCombo, maxCombo);
                _inGameUIController.ComboPanel.SetComboText(currCombo);
            },
            (remaining, duration) =>
            {
                _inGameUIController.ComboPanel.SetComboRushGague(remaining, duration);
                _inGameUIController.MissionPanel.SetRushActive(remaining > 0f);
            },
            (prevTier, currTier) =>
            {
                _cameraController.SetUrgent(currTier >= PursuerState.MAX_TIER - 1);
                _inGameUIController.PursuerPanel.SetTier(currTier);
            },
            OnFailure,
            OnSuccess);
        _missionSystem = _levelManager.MissionSystem;
        if (_levelManager?.Player != null) _irisMaskTransition.Initialize(_levelManager.Player.transform);
        _irisMaskTransition.TransitionIn(null); // OutGame에서 닫힌 채로 진입 → 화면 열기

        if (SoundManager.Instance != null) _inGameStateController.OnStateChanged += SoundManager.Instance.OnStateChanged;
        _inGameStateController.OnStateChanged += _inGameUIController.OnStateChanged;
        _inGameStateController.OnStateChanged += _levelManager.OnStateChanged;
        _inGameStateController.OnStateChanged += OnStateChanged;
        _pointerInput.OnPress += OnFirstPress;
        if (_levelManager?.Player != null)
        {
            // _levelManager.Player.IsInputEnabled = false;
            _levelManager.Player.SlingBehaviour.OnShootSling += OnFirstSlingshot;
            _missionSystem.OnChainChanged += _inGameUIController.MissionPanel.SetChainProgress;
        }
        if (_missionSystem != null)
        {
            var missionPanel = _inGameUIController.MissionPanel;
            _missionSystem.OnMissionStarted += missionPanel.SetMission;
            _missionSystem.OnProgressChanged += missionPanel.SetProgress;
            _missionSystem.OnTimerChanged += missionPanel.SetTimer;
            _missionSystem.OnProgressReset += missionPanel.ResetProgress;
        }

        _inGameStateController.ChangeState(InGameState.Wait);
    }

    // ========== 상태 전환 ==========

    private void OnFirstPress(Vector2 _)
    {
        _pointerInput.OnPress -= OnFirstPress;
        _inGameUIController.WaitPanel.SetVisible(false);
        // _inGameUIController.TouchBlockPanel.SetVisible(false);
        // if (_levelManager?.Player != null) _levelManager.Player.IsInputEnabled = true;
    }

    private void OnFirstSlingshot()
    {
        _levelManager.Player.SlingBehaviour.OnShootSling -= OnFirstSlingshot;
        _inGameStateController.ChangeState(InGameState.Play);
    }

    private void OnStateChanged(InGameState prev, InGameState curr)
    {
        if (curr == InGameState.Play) _isTimerRunning = true;
        else _isTimerRunning = false;
    }

    private void OnFailure()
    {
        var resultData = BuildResultData();
        _inGameUIController.ShowResultPanel(resultData);
        _inGameStateController.ChangeState(InGameState.Failure);
    }

    private void OnSuccess()
    {
        var resultData = BuildResultData();
        _inGameUIController.ShowResultPanel(resultData);
        _inGameStateController.ChangeState(InGameState.Success);
    }

    private GameResultData BuildResultData()
    {
        var player = _levelManager.Player;

        return new GameResultData
        {
            Kills = player != null ? player.KillCount : 0,
            MaxCombo = player != null ? player.Combo.MaxCount : 0,
            Time = _timer,
        };
    }
}
