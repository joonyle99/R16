using DG.Tweening;
using UnityEngine;
using JoonyleGameDevKit;

public enum PursuerAnimationState { Sleep, Idle, Hit, Climb }

/// <summary>
/// 추격자 월드 오브젝트. PursuerState.OnTierChanged를 받아 화면 기준 위치로 이동한다.
/// Camera.ViewportToWorldPoint를 LateUpdate에서 매 프레임 계산하여 카메라 스크롤에 추종한다.
/// </summary>
public class PursuerActor : MonoBehaviour
{
    private static readonly int SLEEP = Animator.StringToHash("Sleep");
    private static readonly int IDLE  = Animator.StringToHash("Idle");
    private static readonly int HIT   = Animator.StringToHash("Hit");
    private static readonly int CLIMB = Animator.StringToHash("Climb");
    
    private static readonly int IDLE_RED  = Animator.StringToHash("Idle_Red");
    private static readonly int HIT_RED   = Animator.StringToHash("Hit_Red");
    private static readonly int CLIMB_RED = Animator.StringToHash("Climb_Red");

    [SerializeField] private float _fixedViewportX = 0.5f;
    [SerializeField] private float _tier1ViewportY = -0.1f;
    [SerializeField] private float _tier2ViewportY = 0.05f;
    [SerializeField] private float _tier3ViewportY = 0.25f;
    [SerializeField] private float _moveDuration = 0.4f;
    [SerializeField] private Ease _easeDown = Ease.OutBack;
    [SerializeField] private Ease _easeUp = Ease.InOutQuad;
    [SerializeField] private float _hiddenViewportY = -0.6f; // 화면 아래로 완전히 숨는 뷰포트 위치 (인트로 등)

    public Animator Animator { get; private set; }

    private Camera _camera;
    private float _baseOrthoSize; // 줌과 무관한 기준 크기 — 줌인/아웃 시 추격자가 따라 올라오지 않게 한다
    private int _currTier;
    private float _currViewportY;
    private bool _isSleeping;
    private Tweener _tween;

    private StateMachine<PursuerActor> _fsm;

    private PursuerEyeTracker _pursuerEyeTreacker;

    public void Tick(float deltaTime)
    {
        _fsm?.Update(deltaTime);
        _pursuerEyeTreacker?.Tick(deltaTime);
    }

    public void LateTick()
    {
        if (_camera == null) return;
        // ViewportToWorldPoint는 현재(줌된) orthographicSize를 쓰므로 줌인 시 추격자가 따라 올라온다.
        // 기준 크기(_baseOrthoSize)로 직접 환산해 줌과 무관한 화면 위치를 유지한다.
        var camPos = _camera.transform.position;
        var worldX = camPos.x + (_fixedViewportX - 0.5f) * 2f * _baseOrthoSize * _camera.aspect;
        var worldY = camPos.y + (_currViewportY - 0.5f) * 2f * _baseOrthoSize;
        transform.position = new Vector3(worldX, worldY, 0f);
    }

    private void OnDestroy() => _tween?.Kill();

    public void Initialize(Camera camera, float baseOrthoSize, PlayerBehaviour player, int startTier)
    {
        Animator = GetComponentInChildren<Animator>();

        _camera = camera;
        _baseOrthoSize = baseOrthoSize;
        _currTier = startTier;
        _currViewportY = ViewportYForTier(startTier);
        _isSleeping = true;

        _fsm = new StateMachine<PursuerActor>(this);
        _fsm.AddState(new PursuerSleepState());
        _fsm.AddState(new PursuerIdleState());
        _fsm.AddState(new PursuerClimbState());
        _fsm.AddState(new PursuerHitState());

        _pursuerEyeTreacker = GetComponentInChildren<PursuerEyeTracker>();
        _pursuerEyeTreacker?.Initialize(player.Rigid);

        // ChangeState<PursuerSleepState>();

        TransitionToTierState();
    }

    public void ChangeState<TState>() where TState : StateBase<PursuerActor> => _fsm.ChangeState<TState>();
    
    // ========== ... ==========

    public void OnStateChanged(InGameState prev, InGameState curr)
    {
        _isSleeping = false; // curr == InGameState.Wait;
        TransitionToTierState();
    }

    // ========== ... ==========

    public void MoveToTier(int fromTier, int toTier)
    {
        _currTier = toTier;

        var ease = toTier < fromTier ? _easeDown : _easeUp;
        _tween?.Kill();
        _tween = DOTween.To(
            () => _currViewportY,
            y => _currViewportY = y,
            ViewportYForTier(toTier),
            _moveDuration
        ).SetEase(ease);

        _pursuerEyeTreacker?.SetCrazy(toTier >= PursuerState.MAX_TIER);

        if (toTier > fromTier) ChangeState<PursuerClimbState>();
        else if (toTier < fromTier) ChangeState<PursuerHitState>();
    }

    // 즉시 화면 밖(아래)으로 내려 숨긴다. 카메라 뷰포트 기준이라 카메라가 어디를 보든 보이지 않는다.
    public void HideBelowScreen()
    {
        _tween?.Kill();
        _currViewportY = _hiddenViewportY;
    }

    // 현재 티어 위치로 부드럽게 올라온다 (인트로 종료 후 "밑에서 등장" 연출).
    public void RiseToTier(float duration)
    {
        _tween?.Kill();
        _tween = DOTween.To(
            () => _currViewportY,
            y => _currViewportY = y,
            ViewportYForTier(_currTier),
            duration
        ).SetEase(_easeUp);
    }

    public void TransitionToTierState()
    {
        if (_isSleeping) ChangeState<PursuerSleepState>();
        else ChangeState<PursuerIdleState>();
    }

    private float ViewportYForTier(int tier) => tier switch
    {
        1 => _tier1ViewportY,
        2 => _tier2ViewportY,
        _ => _tier3ViewportY,
    };

    // ========== ... ==========

    public void PlayAnimation(PursuerAnimationState state)
    {
        var red = _currTier >= PursuerState.MAX_TIER;
        
        switch (state)
        {
            case PursuerAnimationState.Sleep: PlayAnimation(SLEEP);                      break;
            case PursuerAnimationState.Idle:  PlayAnimation(red ? IDLE_RED  : IDLE);    break;
            case PursuerAnimationState.Hit:   PlayAnimation(red ? HIT_RED   : HIT);     break;
            case PursuerAnimationState.Climb: PlayAnimation(red ? CLIMB_RED : CLIMB);   break;
        }
    }

    public void PlayAnimation(int stateHash, float crossFade = 0f) => Animator.CrossFade(stateHash, crossFade);

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var cam = Camera.main;
        if (cam == null) return;

        var tiers = new (int tier, float y, Color color)[]
        {
            (1, _tier1ViewportY, Color.green),
            (2, _tier2ViewportY, Color.yellow),
            (3, _tier3ViewportY, Color.red),
        };

        foreach (var (tier, viewportY, color) in tiers)
        {
            Gizmos.color = color;
            var worldPos = cam.ViewportToWorldPoint(new Vector3(_fixedViewportX, viewportY, 0f));
            Gizmos.DrawSphere(new Vector3(worldPos.x, worldPos.y, 0f), 0.2f);
        }
    }
#endif
}
