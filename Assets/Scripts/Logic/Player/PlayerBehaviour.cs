using System;
using UnityEngine;
using DG.Tweening;
using JoonyleGameDevKit;
using System.Collections;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public enum PlayerAnimationState
{
    Idle,
    Jump_1,
    Roll,
    Wall,
    Stun,
    Die,
}

public sealed class PlayerBehaviour : SlingEntity
{
    public CameraController CameraController { get; private set; }
    public IPointerInput PointerInput { get; private set; }

    [Space]

    [SerializeField] private float _launchPauseDuration = 0.06f;
    [SerializeField] private float _bouncePauseDuration = 0.05f;
    [SerializeField] private float _enemyHitPauseDuration = 0.06f;
    public float LaunchPauseDuration => _launchPauseDuration;
    public float BouncePauseDuration => _bouncePauseDuration;

    [SerializeField] private float _knockbackX = 12f;
    [SerializeField] private float _knockbackY = 8f;
    [SerializeField] private float _stunDuration = 1f;
    [SerializeField] private float _invincibleDuration = 1.5f;
    [SerializeField] private float _propelledHitShake = 0.3f;
    [SerializeField] private float _blinkInterval = 0.08f;

    [SerializeField] private float _minAimSlowFill = 0.2f;

    [SerializeField] private float _comboRushPreEffectDelay = 0.5f;
    [SerializeField] private float _comboRushPre2EffectDelay = 0f;
    [SerializeField] private float _comboRushPostEffectDelay = 0.5f;
    [SerializeField] private float _comboRushPost2EffectDelay = 0f;
    [SerializeField] private float _comboRushFreezeDuration = 0.18f; // 발동 시 완전 정지 시간 (실시간)
    [SerializeField] private float _comboRushZoomScale = 0.8f; // 줌 배율 (1보다 작을수록 줌인)
    [SerializeField] private float _comboRushZoomInDuration = 0.05f; // 줌인 시간
    [SerializeField] private float _comboRushZoomOutDuration = 0.25f; // 줌아웃 (복귀) 시간
    [SerializeField] private float _comboRushConstantShake = 0.2f; // 발동 시 카메라 셰이크 강도
    [SerializeField] private float _comboRushHoverDuration = 0.25f; // 발동 직후 공중에 잠시 멈추는 시간 (중력 유예 → 조준할 여유)

    [SerializeField] private float _energySfxDelay = 0.2f;

    public bool IsPropelled => _fsm?.CurrState is PlayerAirState airState && airState.IsEffectivelyPropelled;

    private float _stunTimer;
    public bool IsStunned => _stunTimer > 0f;

    private bool _blockAimUntilLanding;
    private bool _fastTapCancelUsed;
    private bool _isStartingGame; // 시작 버튼 히트 후 프리즈 (입력/물리/FSM 정지)

    private bool _isAiming;
    private bool _aimStartedGrounded; // 진입 시 접지 여부 — 착지 감지 + HoldSoftSquash/Restore 분기
    private Vector2 _lastDragOffset; // 재조준 간 유지 (의도적으로 리셋하지 않음)

    private ComboSystem _combo;
    public ComboSystem Combo => _combo;

    public int KillCount { get; private set; }

    private Coroutine _invincibleCoroutine;

    private PlatformerSensor _platformerSensor;
    public PlatformerSensor PlatformerSensor => _platformerSensor;

    private PlayerDisplay _playerDisplay;
    public PlayerDisplay PlayerDisplay => _playerDisplay;

    private PlayerStatusVfx _playerStatusVfx;
    public PlayerStatusVfx PlayerStatusVfx => _playerStatusVfx;

    private PointerInputVisualizer _pointerVisualizer;
    public PointerInputVisualizer PointerVisualizer => _pointerVisualizer;

    private SquashStretch _squashStretch;
    public SquashStretch SquashStretch => _squashStretch;

    private Muffler _muffler;
    public Muffler Muffler => _muffler;

    private StateMachine<PlayerBehaviour> _fsm;
    public StateMachine<PlayerBehaviour> FSM => _fsm;

    private ComboRushHitbox _comboRushHitBox;
    private Collider2D _comboRushCollider;

    private bool _wasPropelled;

    private float _comboRushTimer;
    public float ComboRushRemaining => _comboRushTimer;
    public float ComboRushDuration => SlingBehaviour.Config.comboRushDuration;
    public bool IsComboRushActive => _comboRushTimer > 0f;
    private bool _isComboRushIntro; // 러쉬 발동 연출(줌인/이펙트/정지) 중 — 조준 차단
    private float _comboRushHoverTimer; // >0 동안 중력 유예 (러쉬 진입 호버)

    private Vector2 _lastFixedVelocity; // 충돌 해소로 깎이기 전(FixedTick 끝) 속도 — 러쉬 파괴 판정에 사용

    // 러쉬 중 파괴 조건: 위로 상승 중이거나 옆으로 빠르게 이동 중일 때만 (천천히 떨어지거나 앉아있으면 파괴 안 함)
    // 충돌 콜백 시점의 Rigid.linearVelocity는 이미 충돌로 깎여 있을 수 있어, 충돌 직전 속도와 현재 속도 중 더 빠른 쪽으로 판정한다.
    private bool IsRushSmashing
    {
        get
        {
            var config = SlingBehaviour.Config;
            return IsSmashSpeed(_lastFixedVelocity, config) || IsSmashSpeed(Rigid.linearVelocity, config);
        }
    }

    private static bool IsSmashSpeed(Vector2 vel, SlingConfig config)
        => vel.y > config.comboRushSmashUpSpeed || Mathf.Abs(vel.x) > config.comboRushSmashSideSpeed;

    public bool IsInputEnabled { get; set; } = true;

    public bool CanAim => !IsStunned
                        && !_blockAimUntilLanding
                        && !_isComboRushIntro
                        && (SlingBehaviour.HasCharge || IsComboRushActive)
                        && (FSM.CurrState is PlayerGroundState || (FSM.CurrState is PlayerAirState air && !air.IsHitPausing));

    public event Action OnLanded;
    public event Action<EnemyType> OnKilled; // 추진 처치 시(러쉬 포함) — 미션 집계용

    private static readonly int IDLE = Animator.StringToHash("Idle");
    private static readonly int JUMP_1 = Animator.StringToHash("Jump_1");
    private static readonly int ROLL = Animator.StringToHash("Roll");
    private static readonly int WALL = Animator.StringToHash("Wall");
    private static readonly int STUN = Animator.StringToHash("Stun");
    private static readonly int DIE = Animator.StringToHash("Die");
    
    private static readonly float HIT_COLOR_MIN = 0f;
    private static readonly float HIT_COLOR_MAX = 0.7f;

    private Tween _hitColorTween;

    private bool _canBeginAim;
    private bool _aimSlowStartedThisPress;

    private bool _isAimSlowing;
    private float _effectiveTimeScale = 1f;
    private float _defaultDragThreshold;
    private Coroutine _aimSlowCoroutine;
    private float _aimElapsed;
    private bool _energySfxPlayed;

    private Action<int, int> _onComboChanged;
    private Action<float, float> _onComboRushChanged;

    private void OnDestroy()
    {
        if (_combo != null)
        {
            // _combo.OnComboRushThresholdReached -= ActivateComboRush;
            _combo.OnComboChanged -= _onComboChanged;
        }

        if (PointerInput != null)
        {
            PointerInput.OnPress -= OnPointerPress;
            PointerInput.OnRelease -= OnPointerRelease;
        }
    }

    public void Initialize(CameraController cameraController, IPointerInput pointerInput, Action<int, int> onComboChanged, Action<float, float> onComboRushChanged, Action<int> onDamaged, Action onDead)
    {
        _platformerSensor = GetComponent<PlatformerSensor>();
        _platformerSensor.Initialize();

        InitSlingEntity(onDamaged, onDead, _platformerSensor.GroundLayer, _platformerSensor.PlatformLayer);

        CameraController = cameraController;
        CameraController.InitializeByMode(transform);
        PointerInput = pointerInput;
        PointerInput.OnPress += OnPointerPress;
        PointerInput.OnRelease += OnPointerRelease;
        _defaultDragThreshold = pointerInput.DragThresholdScreenRadius;

        _combo = new ComboSystem();
        _combo.Initialize(SlingBehaviour.Config.comboRushThreshold);
        _combo.OnComboChanged += onComboChanged;
        // _combo.OnComboRushThresholdReached += ActivateComboRush;
        _onComboChanged = onComboChanged;
        _onComboRushChanged = onComboRushChanged;

        _playerDisplay = GetComponentInChildren<PlayerDisplay>();
        _playerDisplay.Initialize(SlingBehaviour, _combo, () => IsComboRushActive);

        _playerStatusVfx = GetComponentInChildren<PlayerStatusVfx>();
        _playerStatusVfx.Initialize(Rigid);

        _pointerVisualizer = FindFirstObjectByType<PointerInputVisualizer>();
        _pointerVisualizer.Initialize(pointerInput, () => CanAim && _canBeginAim);

        _squashStretch = GetComponent<SquashStretch>();
        _squashStretch.Initialize(Pivot, Animator.transform);

        _muffler = GetComponentInChildren<Muffler>();
        _muffler.Initialize();

        _fsm = new StateMachine<PlayerBehaviour>(this);
        _fsm.AddState(new PlayerGroundState());
        _fsm.AddState(new PlayerAirState());

        _comboRushHitBox = GetComponentInChildren<ComboRushHitbox>(true);
        _comboRushHitBox?.Initialize(this);
        _comboRushCollider = _comboRushHitBox.GetComponent<Collider2D>();
        if (_comboRushCollider != null) _comboRushCollider.enabled = false;

        ChangeState<PlayerAirState>();
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        if (IsDead) return;
        // if (IsDead || _isStartingGame) return;

        base.FixedTick(fixedDeltaTime);

        _platformerSensor?.FixedTick(fixedDeltaTime);

        // 러쉬 진입 호버: 짧은 시간 동안 중력을 유예해 공중에 멈춘다 (발동 직후 무력하게 추락하는 느낌 방지)
        if (_comboRushHoverTimer > 0f)
            _comboRushHoverTimer = Mathf.Max(0f, _comboRushHoverTimer - fixedDeltaTime);
        else if (_platformerSensor != null && !_platformerSensor.IsGrounded)
            ApplyGravity(fixedDeltaTime);

        _fsm?.FixedUpdate(fixedDeltaTime);

        // 물리 시뮬레이션(충돌 해소) 직전 속도를 저장 — 충돌 콜백에서 깎이기 전 값으로 러쉬 파괴 판정
        _lastFixedVelocity = Rigid.linearVelocity;
    }

    public override void Tick(float deltaTime)
    {
        if (IsDead) return;
        // if (IsDead || _isStartingGame) return;

        base.Tick(deltaTime);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 치트키: 스페이스바로 콤보 러쉬 강제 발동
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            ActivateComboRush();
#endif

        if (_stunTimer > 0f)
        {
            _stunTimer = Mathf.Max(0f, _stunTimer - deltaTime);
            if (_stunTimer == 0f)
            {
                _playerStatusVfx.HideStun();
                PlayPlayerAnimation(PlayerAnimationState.Idle);
            }
        }

        if (_comboRushTimer > 0f)
        {
            // 발동 연출(줌인/프리즈/이펙트) 중에는 시간을 깎지 않고, 연출이 끝난 뒤부터 카운트다운한다
            if (!_isComboRushIntro) _comboRushTimer = Mathf.Max(0f, _comboRushTimer - Time.unscaledDeltaTime);
            if (_comboRushTimer > 0f) _onComboRushChanged?.Invoke(_comboRushTimer, ComboRushDuration); // 러쉬 중 게이지를 남은 시간 카운트다운으로 갱신
            else { _onComboRushChanged?.Invoke(0f, ComboRushDuration); _combo.Reset(); _playerDisplay.RefreshDisplay(); _playerDisplay.HideComboRushEffect(); } // 러쉬 종료(양수→0) 프레임에 콤보 리셋 → 일반 게이지가 빈 상태로 복귀 + 차지 오브 다시 표시
        }

        // 추진 상태의 진입/이탈 엣지 (grace 포함, IsPropelled 정의를 그대로 사용)
        // 이탈 시 점프 애니메이션 복귀만 담당한다.
        var propelled = IsPropelled;
        if (propelled != _wasPropelled)
        {
            if (!propelled && !IsStunned) PlayPlayerAnimation(PlayerAnimationState.Jump_1);
            _wasPropelled = propelled;
        }

        // 추진 이펙트는 콤보 러쉬 중에는 띄우지 않는다 (러쉬 전용 이펙트로 대체)
        if (IsPropelled && !IsComboRushActive) _playerStatusVfx.ShowPropelled();
        else _playerStatusVfx.HidePropelled();

        // 콤보 러쉬가 켜져 있는 동안 전용 이펙트 유지 (Set이 중복 토글을 막아 매 프레임 호출해도 안전)
        if (IsComboRushActive) _playerStatusVfx.ShowComboRush();
        else _playerStatusVfx.HideComboRush();

        // 콤보 러쉬 전용 히트박스 콜라이더 토글 — 러쉬 중에만 활성화해 확장 판정 적용
        if (_comboRushCollider != null) _comboRushCollider.enabled = IsComboRushActive;

        _playerStatusVfx.Tick(deltaTime);

        // 조준은 상태가 아니라 Ground/Air 위에 겹쳐지는 오버레이다.
        // 물리 전이(착지/낙하)는 조준 중에도 FSM이 그대로 처리하고(Air.Update의 착지 가드 재사용),
        // budget 틱(Air.FixedUpdate)도 FixedTick에서 계속 흐른다.
        // UpdateAiming은 그 위에서 조준 입력·궤적·발사만 담당한다.
        _fsm?.Update(deltaTime);

        // 탭으로 멈추기
        // if (!_isAiming
        //     && PointerInput.JustFastTapped
        //     && !_fastTapCancelUsed
        //     && FSM.CurrState is PlayerAirState tappedAirState
        //     && tappedAirState.IsEffectivelyPropelled
        //     && !tappedAirState.IsHitPausing)
        // {
        //     _fastTapCancelUsed = true;
        //     tappedAirState.CancelBudget();
        //     Rigid.linearVelocity = Vector2.zero;
        // }

        if (!_isAiming && CanAim && PointerInput.IsDragging && _canBeginAim) BeginAim();
        if (_isAiming) UpdateAiming();

        _pointerVisualizer?.Tick(deltaTime);
    }

    public void ChangeState<TState>() where TState : StateBase<PlayerBehaviour>
    {
        _fsm.ChangeState<TState>();
    }

    // ============ ... ============

    public void NotifyLanded()
    {
        _blockAimUntilLanding = false;
        _fastTapCancelUsed = false;
        _combo.Reset();
        OnLanded?.Invoke();
    }

    // ============ ... ============

    private void OnPointerPress(Vector2 _)
    {
        if (!IsInputEnabled || !CanAim) return;
        _canBeginAim = true;
        _aimSlowStartedThisPress = false;
    }

    private void OnPointerRelease(Vector2 _)
    {
        if (!IsInputEnabled) return;
        CancelAimSlow();
        _canBeginAim = false;
    }

    protected override void OnDamaged(int damage, Vector2 sourcePos)
    {
        base.OnDamaged(damage, sourcePos);

        var dirX = Rigid.position.x >= sourcePos.x ? 1f : -1f;
        Rigid.linearVelocity = new Vector2(dirX * _knockbackX, _knockbackY);

        if (_invincibleCoroutine != null) StopCoroutine(_invincibleCoroutine);
        _invincibleCoroutine = StartCoroutine(InvincibleRoutine());
    }

    protected override void OnDead()
    {
        base.OnDead();

        _playerStatusVfx.HideAll();
        PlayPlayerAnimation(PlayerAnimationState.Die);
        Rigid.simulated = false;
    }

    private void OnPropelledHit(EnemyBehaviour enemy)
    {
        // 콤보 러쉬 중에는 차지가 소모되지도 획득되지도 않고, 콤보도 쌓지 않는다
        if (!IsComboRushActive)
        {
            _combo.Add();
            OnKilled?.Invoke(enemy.EnemyType); // 러쉬 중에도 미션엔 집계 (파워 윈도우)
            
            if (SlingBehaviour.CurrCharges < SlingBehaviour.TotalCharges)
            {
                _playerDisplay.PlayChargeAbsorb(enemy.transform.position, Rigid.linearVelocity, null);
                SlingBehaviour.AddCharge();
            }
        }
        KillCount++;
        StartCoroutine(HitEffectRoutine());
        if (IsComboRushActive && enemy.TryGetComponent<RushLaunchable>(out var rushLaunchable))
        {
            rushLaunchable.Launch();
            enemy.Kill();
        }
        else enemy.TakeDamage(enemy.MaxHp, enemy.Rigid.position);
        var hitPos = (Vector2)(this.Visual.position + enemy.Visual.position) * 0.5f;
        EffectManager.Instance?.Play(VfxType.Attack, hitPos);
        SoundManager.Instance.PlaySfx(SfxType.Attack, 0.8f);

        // _muffler.PlayGrabAnimation(enemy.transform.position, _enemyHitPauseDuration * 0.9f, null);
    }

    private void OnPropelledHit(InteractableButton button, Vector2 contactPoint)
    {
        var hitPos = ((Vector2)this.Visual.position + contactPoint) * 0.5f;
        EffectManager.Instance?.Play(VfxType.Attack, hitPos);
        SoundManager.Instance.PlaySfx(SfxType.Attack, 0.8f);

        if (button is GameStartButton)
        {
            _isStartingGame = true;
            _playerStatusVfx.HideAll();
            if (_isAiming) EndAim();
            CancelAimSlow();
        }

        button.Interact();
    }

    private void OnDriftingHit(EnemyBehaviour enemy)
    {
        if (IsInvincible || IsComboRushActive) return;

        CancelAimSlow();

        var dirX = Rigid.position.x >= enemy.Rigid.position.x ? 1f : -1f;
        Rigid.linearVelocity = new Vector2(dirX * _knockbackX, _knockbackY);

        if (_invincibleCoroutine != null) StopCoroutine(_invincibleCoroutine);
        _invincibleCoroutine = StartCoroutine(InvincibleRoutine());

        _stunTimer = _stunDuration;
        _playerStatusVfx.ShowStun();
        PlayPlayerAnimation(PlayerAnimationState.Stun);
        SoundManager.Instance.PlaySfx(SfxType.Damaged, 0.8f);

        // if (!_platformerSensor.IsGrounded)
        //     _blockAimUntilLanding = true;

        // if (_isAiming) EndAim(); // 조준 중 피격 시 조준 취소 (현재 상태는 그대로 유지)
        if (_isAiming)
        {
            _canBeginAim = false;

            EndAim();
        }
    }

    public void ActivateComboRush()
    {
        if (IsComboRushActive || _isComboRushIntro || IsStunned || IsDead) return;

        _comboRushTimer = SlingBehaviour.Config.comboRushDuration;
        StartCoroutine(ComboRushIntroRoutine());
    }

    // ========= ... =========

    private void BeginAim()
    {
        _isAiming = true;
        _aimStartedGrounded = _platformerSensor.IsGrounded && !IsComboRushActive;
        SoundManager.Instance.PlaySfx(SfxType.AimDrag);

        _aimElapsed = 0f;
        _energySfxPlayed = false;

        // 콤보 러쉬 중에는 에임 슬로우를 걸지 않는다 (러쉬는 풀스피드 유지)
        if (!_aimSlowStartedThisPress && !IsComboRushActive)
        {
            _aimSlowStartedThisPress = true;
            if (_aimSlowCoroutine != null) StopCoroutine(_aimSlowCoroutine);
            _aimSlowCoroutine = StartCoroutine(AimSlowFadeRoutine());
        }

        _playerDisplay.BeginAimPreview(_aimStartedGrounded);
        _playerStatusVfx.ShowEnergy();
        if (_aimStartedGrounded)
            _squashStretch.HoldSoftSquash();
    }

    private void UpdateAiming()
    {
        // 조준선을 일정 시간(_energySfxDelay) 이상 유지했을 때만 에너지 SFX 재생
        if (!_energySfxPlayed)
        {
            _aimElapsed += Time.unscaledDeltaTime;
            if (_aimElapsed >= _energySfxDelay)
            {
                _energySfxPlayed = true;
                SoundManager.Instance.PlaySfx(SfxType.Energy);
            }
        }

        // 조준 중 착지 시 그라운드 모드로 전환 (궤적·발사 방식 동기화)
        var isNowGrounded = _platformerSensor.IsGrounded && !IsComboRushActive;
        if (!_aimStartedGrounded && isNowGrounded)
        {
            _aimStartedGrounded = true;
            _squashStretch.HoldSoftSquash();
        }

        var pointerInput = PointerInput;
        var camera = CameraController.MainCamera;
        var worldUnitsPerPixel = camera.orthographicSize * 2f / Screen.height;

        if (pointerInput.IsDragging)
        {
            _lastDragOffset = pointerInput.GetScreenDragDelta * worldUnitsPerPixel;
            if (!Mathf.Approximately(_lastDragOffset.x, 0f))
                SetFacingDir(_lastDragOffset.x < 0f); // 드래그 반대 방향 = 발사 방향
        }

        if (pointerInput.JustReleased)
        {
            if (!pointerInput.JustTapped)
            {
                EndAim();
                _comboRushHoverTimer = 0f; // 발사하면 호버 종료 → 중력/관성 정상 복귀
                SlingBehaviour.ShootSling(_lastDragOffset, !IsComboRushActive && !_aimStartedGrounded, _aimStartedGrounded);
                ChangeState<PlayerAirState>(); // Air 재진입 → Enter의 ConsumeSling이 새 budget 생성
            }
            else
            {
                EndAim(); // 탭 = 취소, 현재 상태 유지
            }

            return;
        }
        else if (!pointerInput.IsDragging)
        {
            EndAim(); // 드래그가 임계 안으로 되돌아옴 = 취소
            _squashStretch.Restore();

            return;
        }

        SlingBehaviour.ShowTrajectory(_lastDragOffset, _aimStartedGrounded, IsComboRushActive);
    }

    private void EndAim()
    {
        if (!_isAiming) return;
        _isAiming = false;

        SlingBehaviour.HideTrajectory();
        _playerDisplay.EndAimPreview();
        _playerStatusVfx.HideEnergy();
        if (_aimStartedGrounded)
            _squashStretch.Restore();
    }

    // ============ ... ============

    private IEnumerator InvincibleRoutine()
    {
        IsInvincible = true;

        Material.SetFloat("_Amount", HIT_COLOR_MAX);
        _hitColorTween?.Kill();
        _hitColorTween = DOVirtual.Float(HIT_COLOR_MAX, HIT_COLOR_MIN, _blinkInterval, v => Material.SetFloat("_Amount", v))
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .SetLink(gameObject);

        yield return new WaitForSecondsRealtime(_invincibleDuration);

        _hitColorTween?.Kill();
        _hitColorTween = null;
        Material.SetFloat("_Amount", HIT_COLOR_MIN);

        _invincibleCoroutine = null;
        
        IsInvincible = false;
    }

    private IEnumerator HitEffectRoutine()
    {
        var kickDir = (Vector2)Rigid.linearVelocity.normalized;
        CameraController.Shake(kickDir, _propelledHitShake);
        if (!IsComboRushActive) // 콤보 러쉬 중에는 히트 스탑 생략
            yield return HitStopRoutine(_enemyHitPauseDuration);
    }

    public IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        // 인트로가 timeScale을 소유 중이면 덮어쓰지 않는다
        if (!_isComboRushIntro) Time.timeScale = _isAimSlowing ? _effectiveTimeScale : 1f;
    }

    private IEnumerator HitPauseRoutine(float duration, Vector2 velocity, Action onResume)
    {
        Rigid.linearVelocity = Vector2.zero;
        Rigid.constraints = RigidbodyConstraints2D.FreezeAll;
        yield return new WaitForSeconds(duration);
        Rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
        Rigid.linearVelocity = velocity;
        onResume?.Invoke();
    }

    private IEnumerator AimSlowFadeRoutine()
    {
        _isAimSlowing = true;
        _effectiveTimeScale = SlingBehaviour.Config.aimSlowScale;
        Time.timeScale = _effectiveTimeScale;
        _pointerVisualizer?.SetPressMarkerFill(1f);
        PointerInput.DragThresholdScreenRadius = _defaultDragThreshold;

        yield return new WaitForSecondsRealtime(SlingBehaviour.Config.aimSlowHoldDuration);

        var config = SlingBehaviour.Config;
        var elapsed = 0f;
        var from = config.aimSlowScale;

        while (elapsed < config.aimSlowFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / config.aimSlowFadeDuration);
            _effectiveTimeScale = Mathf.Lerp(from, 1f, t);
            Time.timeScale = _effectiveTimeScale;
            var fill = Mathf.Max(1f - t, _minAimSlowFill);
            _pointerVisualizer?.SetPressMarkerFill(fill);
            PointerInput.DragThresholdScreenRadius = _defaultDragThreshold * fill;
            yield return null;
        }

        _effectiveTimeScale = 1f;
        Time.timeScale = _effectiveTimeScale;
        _pointerVisualizer?.SetPressMarkerFill(_minAimSlowFill);
        PointerInput.DragThresholdScreenRadius = _defaultDragThreshold * _minAimSlowFill;
        _aimSlowCoroutine = null;
    }

    private IEnumerator ComboRushIntroRoutine()
    {
        {
            
        }
        
        yield return new WaitForSecondsRealtime(_comboRushPre2EffectDelay);
        
        // 1. pre delay 전
        {
            _isComboRushIntro = true;
            _canBeginAim = false; // 연출 중/끝난 직후 살아있는 누름으로 조준이 재개되지 않도록 무효화 (다시 눌러야 함)
            if (_isAiming) EndAim(); // 연출 진입 시 진행 중이던 조준은 취소
            CancelAimSlow(); // 에임 슬로우 코루틴이 정지 연출의 timeScale을 덮어쓰지 않도록 끊는다
            Time.timeScale = 0f;
            _playerStatusVfx.HideAll(); // 러쉬 진입 — 추진/에너지/스턴만 초기화하고 러쉬 전용 이펙트는 유지(토글 경합 방지)
            CameraController.ZoomScale(_comboRushZoomScale, _comboRushZoomInDuration);
        }

        yield return new WaitForSecondsRealtime(_comboRushPreEffectDelay);

        // 2. freeze duration 전
        {
            SoundManager.Instance.PlaySfx(SfxType.Rush, 1f);
            EffectManager.Instance?.Play(VfxType.ComboRush, transform.position);
            CameraController.BeginConstantShake(_comboRushConstantShake);
        }

        yield return new WaitForSecondsRealtime(_comboRushFreezeDuration);

        // 3. post delay 전
        {
            CameraController.StopConstantShake();
        }

        yield return new WaitForSecondsRealtime(_comboRushPostEffectDelay);

        // 4. post delay 후
        {
            // CameraController.Shake(5f);
            // TODO: 여기서 이펙트 터트리기 (분리 요청해서)
            _playerDisplay.RefreshDisplay(); // 러쉬 진입 — 차지 오브 숨김
            // _playerDisplay.ShowComboRushEffect();
            _comboRushHoverTimer = _comboRushHoverDuration; // 잠시 공중에 멈춰 조준할 여유를 준다
            CameraController.ResetZoom(_comboRushZoomOutDuration);
            _isComboRushIntro = false;
        }

        yield return new WaitForSecondsRealtime(_comboRushPost2EffectDelay);

        {
            Rigid.linearVelocity = Vector2.zero; // 발동 직전 하강 관성 제거 → 깔끔한 정지
            Time.timeScale = _isAimSlowing ? _effectiveTimeScale : 1f;
        }
    }

    // ========= ... =========

    public void ApplyGravity(float deltaTime)
    {
        var vel = Rigid.linearVelocity;
        vel.y -= SlingBehaviour.Config.slingGravity * deltaTime;
        Rigid.linearVelocity = vel;
    }

    public void CancelAimSlow()
    {
        _isAimSlowing = false;
        _effectiveTimeScale = 1f;
        // 인트로가 timeScale(정지)을 소유 중이면 덮어쓰지 않는다 — 릴리스로 정지 연출이 풀리는 것을 방지
        if (!_isComboRushIntro) Time.timeScale = _effectiveTimeScale;
        _pointerVisualizer?.SetPressMarkerFill(1f);
        PointerInput.DragThresholdScreenRadius = _defaultDragThreshold;

        if (_aimSlowCoroutine != null)
        {
            StopCoroutine(_aimSlowCoroutine);
            _aimSlowCoroutine = null;
        }
    }

    public new void SetFacingDir(bool facingRight)
    {
        base.SetFacingDir(facingRight);

        _muffler?.SetFacingDir(facingRight);
    }

    public void PauseAndLaunch(float duration, Vector2 velocity, Action onResume)
    {
        StartCoroutine(HitPauseRoutine(duration, velocity, onResume));
    }

    public void PlayPlayerAnimation(PlayerAnimationState state)
    {
        switch (state)
        {
            case PlayerAnimationState.Idle: PlayAnimation(IDLE); break;
            case PlayerAnimationState.Jump_1: PlayAnimation(JUMP_1); break;
            case PlayerAnimationState.Roll: PlayAnimation(ROLL); break;
            case PlayerAnimationState.Wall: PlayAnimation(WALL); break;
            case PlayerAnimationState.Stun: PlayAnimation(STUN); break;
            case PlayerAnimationState.Die: PlayAnimation(DIE); break;
        }
    }

    // ========= ... =========

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsDead || IsStunned) return;

        if (_fsm.CurrState is PlayerAirState airState)
        {            
            airState.OnCollisionEnter(this, collision);

            if (collision.gameObject.GetComponent<InteractableButton>() is { } interactableButton)
            {
                if (IsPropelled) OnPropelledHit(interactableButton, collision.GetContact(0).point);
            }

            // 조준 중 벽킥(히트포즈 발생) → PauseAndLaunch가 조준 오버레이와 엉키지 않도록 조준 종료.
            // 데드 바운스(히트포즈 없음)는 조준을 끊지 않는다.
            if (_isAiming && airState.IsHitPausing)
            {
                _canBeginAim = false;

                EndAim();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (!IsComboRushActive) CheckHit(collider);
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        if (!IsComboRushActive) CheckHit(collider);
    }

    // 콤보 러쉬 전용 히트박스(ComboRushHitbox)에서 호출 — 기본 콜라이더와 이중 감지 방지
    public void CheckComboRushHit(Collider2D collider) => CheckHit(collider);

    private void CheckHit(Collider2D collider)
    {
        if (IsDead) return;

        if (collider.TryGetComponent<EnemyBehaviour>(out var enemy) && !enemy.IsDead)
        {
            // 러쉬 중에는 부위 구분 없이, 충분히 빠를 때만 파괴 — 느리게 떨어지거나 앉아있을 땐 그냥 통과
            if (IsComboRushActive) { if (IsRushSmashing) OnPropelledHit(enemy); }
            // 콤보러쉬가 아닐 때만 부위 판정: 회전 적의 껍질(Armor)은 처치 불가 → 피격
            else if (enemy is RotatingEnemyBehaviour rotating && rotating.GetHitPart(Visual.position) == EnemyHitPart.Armor) OnDriftingHit(enemy);
            else if (IsPropelled) OnPropelledHit(enemy);
            else OnDriftingHit(enemy);
        }
        else if (collider.TryGetComponent<RushLaunchable>(out var rushLaunchable))
        {
            if (IsComboRushActive && IsRushSmashing)
            {
                rushLaunchable.Launch();
                var hitPos = (Vector2)(this.Visual.position + rushLaunchable.transform.position) * 0.5f;
                EffectManager.Instance?.Play(VfxType.Attack, hitPos);
                SoundManager.Instance.PlaySfx(SfxType.Attack, 0.8f);
            }
        }
    }
}
