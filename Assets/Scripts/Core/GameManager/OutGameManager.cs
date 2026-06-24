using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class OutGameManager : MonoBehaviour
{
    private PointerInput _pointerInput;
    private GameStateController<OutGameState> _outGameStateController;
    private CameraController _cameraController;
    private OutGameUIController _outGameUIController;
    private PlayerBehaviour _player;

    [SerializeField] private Transform _cameraPointStore;
    [SerializeField] private Transform _cameraPointLobby;
    [SerializeField] private Transform _cameraPointPractice;
    [SerializeField] private Transform _boundaryStoreLeft; // Store 좌측 경계
    [SerializeField] private Transform _boundaryLobbyStore; // Lobby | Store 경계
    [SerializeField] private Transform _boundaryLobbyPractice; // Store | Practice 경계
    [SerializeField] private Transform[] _cameraPointsPracticeFloor; // Practice 층별 카메라 앵커 (아래→위 순)
    [SerializeField] private IrisMaskTransition _irisMaskTransition;
    [SerializeField] private GameStartButton _gameStartButton;
    [SerializeField] private RelicBoxButton _relicBoxButton;

    [SerializeField] private float _roomEntryMaxSpeed = 8f; // 방 경계를 넘을 때 허용되는 최대 속도 (초과 시에만 감속)
    [SerializeField] private float _floorHysteresis = 0.5f; // 층 경계 깜빡임 방지 마진

    private bool _inStoreRoom;
    private bool _inPracticeRoom;
    private int _currPracticeFloor;

    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        // if (_player != null) _player.OnLanded -= CheckRoomBoundary;
        _outGameStateController.OnStateChanged -= OnStateChanged;
        _outGameStateController.OnStateChanged -= _outGameUIController.OnStateChanged;
        if (SoundManager.Instance != null) _outGameStateController.OnStateChanged -= SoundManager.Instance.OnStateChanged;

        _pointerInput.Dispose();
    }

    private void FixedUpdate()
    {
        var fixedDeltaTime = Time.fixedDeltaTime;
        _player?.FixedTick(fixedDeltaTime);
    }

    private void Update()
    {
        var deltaTime = Time.deltaTime;
        _pointerInput?.Tick(deltaTime);
        _player?.Tick(deltaTime);
        CheckRoomBoundary();
        if (_inStoreRoom) TrackPlayerXInStore();
        if (_inPracticeRoom) TrackFloorYInPractice();
        _gameStartButton?.Tick(deltaTime);
        _relicBoxButton?.Tick(deltaTime);
    }

    private void LateUpdate()
    {
        _cameraController?.LateTick(Time.deltaTime);
    }
    
    private void Initialize()
    {
        _outGameStateController = new GameStateController<OutGameState>();
        _cameraController = FindFirstObjectByType<CameraController>();
        _outGameUIController = FindFirstObjectByType<OutGameUIController>();
        _player = FindFirstObjectByType<PlayerBehaviour>();
        _cameraController.Initialize(CameraMode.OutGame);
        _pointerInput = new PointerInput(_cameraController.MainCamera);
        _outGameUIController.Initialize();
        _player.Initialize(_cameraController, _pointerInput, null, null, null, null);
        // _player.OnLanded += CheckRoomBoundary;
        _irisMaskTransition.Initialize(_player.transform);
        _gameStartButton.Initialize(() => _irisMaskTransition.TransitionOut(() => SceneManager.LoadScene("InGameScene")));
        _relicBoxButton.Initialize(null);

        if (SoundManager.Instance != null) _outGameStateController.OnStateChanged += SoundManager.Instance.OnStateChanged;
        _outGameStateController.OnStateChanged += _outGameUIController.OnStateChanged;
        _outGameStateController.OnStateChanged += OnStateChanged;

        _outGameStateController.ChangeState(OutGameState.Room_Lobby);
    }

    private void OnStateChanged(OutGameState prev, OutGameState curr)
    {
        Transform point = curr switch
        {
            OutGameState.Room_Store    => _cameraPointStore,
            OutGameState.Room_Lobby    => _cameraPointLobby,
            OutGameState.Room_Practice => _cameraPointPractice,
            _                          => null,
        };

        if (point == null) return;

        if (prev == OutGameState.None) _cameraController.SnapToTargetPos(point.position);
        else _cameraController.MoveToTargetPosX(point.position.x);

        _inStoreRoom = curr == OutGameState.Room_Store
            && _boundaryStoreLeft != null
            && _boundaryLobbyStore != null;

        _inPracticeRoom = curr == OutGameState.Room_Practice
            && _cameraPointsPracticeFloor != null 
            && _cameraPointsPracticeFloor.Length > 0;

        if (_inPracticeRoom)
        {
            _currPracticeFloor = NearestFloor(_player.transform.position.y); // 진입 시 현재 층
            _cameraController.MoveToTargetPosY(_cameraPointsPracticeFloor[_currPracticeFloor].position.y);
        }
        else
        {
            _cameraController.MoveToTargetPosY(point.position.y); // 방 고정 Y로 복귀
        }
    }

    private void CheckRoomBoundary()
    {
        if (_player == null) return;

        var playerX = _player.transform.position.x;

        OutGameState targetState;

        if (playerX <= _boundaryLobbyStore.position.x) targetState = OutGameState.Room_Store;
        else if (playerX > _boundaryLobbyStore.position.x && playerX < _boundaryLobbyPractice.position.x) targetState = OutGameState.Room_Lobby;
        else if (playerX >= _boundaryLobbyPractice.position.x) targetState = OutGameState.Room_Practice;
        else targetState = OutGameState.Room_Lobby;

        if (_outGameStateController.CurrState != targetState)
        {
            _player.Rigid.linearVelocity = Vector2.ClampMagnitude(_player.Rigid.linearVelocity, _roomEntryMaxSpeed);
            _outGameStateController.ChangeState(targetState);
        }
    }

    private void TrackPlayerXInStore()
    {
        if (_player == null) return;

        var halfW = _cameraController.CameraWidth * 0.5f;
        var minX = _boundaryStoreLeft.position.x + halfW;
        var maxX = _boundaryLobbyStore.position.x - halfW;
        
        _cameraController.MoveToTargetPosX(Mathf.Clamp(_player.transform.position.x, minX, maxX));
    }

    /// <summary>
    /// Practice 방: 층 경계(이웃 앵커 중점) + 히스테리시스로 한 프레임에 한 층씩 이동
    /// </summary>
    private void TrackFloorYInPractice()
    {
        if (_player == null || _cameraPointsPracticeFloor == null || _cameraPointsPracticeFloor.Length == 0) return;

        var y = _player.transform.position.y;

        if (_currPracticeFloor < _cameraPointsPracticeFloor.Length - 1)
        {
            var up = MidBetweenFloors(_currPracticeFloor, _currPracticeFloor + 1);
            if (y > up + _floorHysteresis) { SetFloor(_currPracticeFloor + 1); return; }
        }
        if (_currPracticeFloor > 0)
        {
            var down = MidBetweenFloors(_currPracticeFloor - 1, _currPracticeFloor);
            if (y < down - _floorHysteresis) { SetFloor(_currPracticeFloor - 1); return; }
        }
    }

    private void SetFloor(int floor)
    {
        _currPracticeFloor = floor;
        _cameraController.MoveToTargetPosY(_cameraPointsPracticeFloor[floor].position.y);
    }
    private int NearestFloor(float y)
    {
        if (_cameraPointsPracticeFloor == null || _cameraPointsPracticeFloor.Length == 0) return 0;

        int floor = 0;
        while (floor < _cameraPointsPracticeFloor.Length - 1 && y > MidBetweenFloors(floor, floor + 1)) floor++;
        return floor;
    }
    private float MidBetweenFloors(int a, int b) => (_cameraPointsPracticeFloor[a].position.y + _cameraPointsPracticeFloor[b].position.y) * 0.5f;
}
