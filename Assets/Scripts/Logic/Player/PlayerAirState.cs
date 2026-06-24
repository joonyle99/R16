using DG.Tweening;
using UnityEngine;
using JoonyleGameDevKit;

public sealed class PlayerAirState : StateBase<PlayerBehaviour>
{
    private SlingConfig _slingConfig;
    private SlingState _slingBudget;
    private bool _isHitPausing;
    private bool _noChargeFeedbackSent;
    private int _bonusKicksLeft;
    private float _graceTimer;

    public bool IsHitPausing => _isHitPausing;
    public bool HasBudget => _slingBudget.Remaining > 0f;
    public bool IsEffectivelyPropelled => HasBudget || _graceTimer > 0f;

    public override void Enter(PlayerBehaviour owner)
    {
        _isHitPausing = false;
        _noChargeFeedbackSent = false;
        _graceTimer = 0f;
        _slingConfig = owner.SlingBehaviour.Config;
        _bonusKicksLeft = _slingConfig.bonusKickCount;

        var wasGrounded = owner.SlingBehaviour.LastShotFromGround;

        if (owner.SlingBehaviour.ConsumeSling())
        {
            var shotDir = owner.SlingBehaviour.LastShotDir;
            owner.SetFacingDir(shotDir.x > 0f);
            _slingBudget = SlingState.Create(owner.Rigid.position, shotDir, _slingConfig, wasGrounded, owner.IsComboRushActive);

            _isHitPausing = true;

            owner.SquashStretch.HoldSquash();
            if (wasGrounded) _slingBudget.Remaining = 0f; // 땅 발사는 추진력 상태 부여 안 함
            owner.PauseAndLaunch(owner.LaunchPauseDuration, _slingBudget.Velocity, () =>
            {
                _isHitPausing = false;
                owner.SquashStretch.PlayStretch();
                owner.PlayPlayerAnimation(wasGrounded ? PlayerAnimationState.Jump_1 : PlayerAnimationState.Roll);
                var flipY = shotDir.y < 0f;
                if (wasGrounded) EffectManager.Instance?.Play(VfxType.Jump_1, owner.transform.position, flipY: flipY);
                else
                {
                    // EffectManager.Instance.Play(VfxType.Jump_2, owner.transform.position, flipY: flipY);
                    var angle = Mathf.Atan2(shotDir.y, shotDir.x) * Mathf.Rad2Deg - 90f;
                    DOVirtual.DelayedCall(0f, () => EffectManager.Instance?.Play(VfxType.Jump_2, owner.transform.position, rotation: angle));
                    DOVirtual.DelayedCall(0.15f, () => EffectManager.Instance?.Play(VfxType.Jump_2, owner.transform.position, rotation: angle));
                    DOVirtual.DelayedCall(0.2f, () => EffectManager.Instance?.Play(VfxType.Jump_2, owner.transform.position, rotation: angle));
                }
            });
        }
        else
        {
            _slingBudget = default;
            _slingBudget.Position = owner.Rigid.position;
            if (!owner.IsStunned) owner.PlayPlayerAnimation(PlayerAnimationState.Idle);
        }
    }

    public override void Exit(PlayerBehaviour owner) { }

    public override void FixedUpdate(PlayerBehaviour owner, float fixedDeltaTime)
    {
        if (_isHitPausing) return;

        bool wasPropelled = HasBudget;

        _slingBudget.Remaining -= fixedDeltaTime;
        _graceTimer = Mathf.Max(0f, _graceTimer - fixedDeltaTime);
        _slingBudget.Total += (owner.Rigid.position - _slingBudget.Position).magnitude;
        _slingBudget.Position = owner.Rigid.position;

        if (wasPropelled && !HasBudget)
        {
            _graceTimer = _slingConfig.propelledGraceDuration;
            // Roll 유지 — grace는 추진 판정 연장이므로 시각적으로도 Roll
        }
    }

    public override void Update(PlayerBehaviour owner, float deltaTime)
    {
        if (_isHitPausing) return;

        // 상승 가드: 히트포즈 해제(Update 단계)와 센서 갱신(FixedUpdate 단계) 사이에
        // 낡은 IsGrounded가 남아 있을 수 있다 — 그대로 믿으면 GroundState가 발사 직후 x속도를 지워버린다
        if (owner.PlatformerSensor.IsGrounded && owner.Rigid.linearVelocity.y <= 0f)
        {
            owner.ChangeState<PlayerGroundState>();
        }
        else if (owner.PointerInput.IsDragging && !owner.SlingBehaviour.HasCharge && !owner.IsComboRushActive && !_noChargeFeedbackSent)
        {
            _noChargeFeedbackSent = true;
            var duration = owner.SlingBehaviour.Config.noChargePauseDuration;
            owner.StartCoroutine(owner.HitStopRoutine(duration));
            owner.PlayerDisplay.ShowNoChargeEffect(duration);
            SoundManager.Instance.PlaySfx(SfxType.NoCharge);
        }
    }

    public void OnCollisionEnter(PlayerBehaviour owner, Collision2D collision)
    {
        if (_isHitPausing) return;
        if (owner.IsComboRushActive) return; // 콤보 러쉬 중에는 벽킥/바운스 없이 직진
        if ((owner.PlatformerSensor.GroundLayer.value & (1 << collision.gameObject.layer)) == 0) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            var normal = collision.GetContact(i).normal;

            if (!SlingSimulator.IsWall(normal, _slingConfig)) continue;

            if (owner.IsComboRushActive)
            {
                // TODO: 벽에 튕겼을때 어떠한 움직임을 주고싶은가?
                // var vel = owner.Rigid.linearVelocity;
                // vel.x = normal.x * _slingConfig.airSlingSpeed;
                // owner.Rigid.linearVelocity = vel;
                // owner.SetFacingDir(vel.x > 0f);
            }
            else if (SlingSimulator.CanBounce(in _slingBudget, _slingConfig))
            {
                // 예산 킥: 조준선이 예측한 튕김 — 추진 상태 유지
                SlingSimulator.Bounce(ref _slingBudget, normal, _slingConfig);
                WallKick(owner, _slingBudget.Velocity, true);
            }
            else if (_bonusKicksLeft > 0 && !owner.IsStunned)
            {
                // 보너스 킥: 예산이 소진돼도 벽에 닿으면 추가 점프 — 조준선에는 표시되지 않는 규칙, 추진 상태 부여 안 함
                _bonusKicksLeft--;
                var vel = SlingSimulator.Kick(normal, _slingConfig.kick);
                WallKick(owner, vel, false);
            }
            else
            {
                var vel = owner.Rigid.linearVelocity;
                vel.x = normal.x * _slingConfig.wallRepelSpeed;
                owner.Rigid.linearVelocity = vel;
                owner.SetFacingDir(vel.x > 0f);
            }

            return;
        }
    }

    /// <summary>
    /// 벽 충돌 시 반동 처리. 히트포즈 후 vel 방향으로 발사하며, 애니메이션·스쿼시·더스트 이펙트를 재생한다.
    /// </summary>
    /// <param name="owner">플레이어</param>
    /// <param name="vel">호출 시점에 계산된 반동 속도 (방향 결정에도 사용)</param>
    /// <param name="isBudgetKick">true = 조준선이 예측한 예산 킥(Roll), false = 보너스 킥(Wall→Jump_1)</param>
    private void WallKick(PlayerBehaviour owner, Vector2 vel, bool isBudgetKick)
    {
        _isHitPausing = true;
        var isFacingRight = vel.x > 0f;
        owner.SetFacingDir(isFacingRight);
        owner.SquashStretch.SetContactSurface(ContactSurface.Wall);
        owner.PlayPlayerAnimation(isBudgetKick ? PlayerAnimationState.Roll : PlayerAnimationState.Wall);
        owner.SquashStretch.HoldSideSquash();
        SoundManager.Instance.PlaySfx(SfxType.Bounce, 1f);
        owner.PauseAndLaunch(owner.BouncePauseDuration, vel, () =>
        {
            _isHitPausing = false;
            owner.SquashStretch.PlayStretch();
            if (!isBudgetKick) owner.PlayPlayerAnimation(PlayerAnimationState.Jump_1);
            EffectManager.Instance?.Play(VfxType.Dust, owner.transform.position, isFacingRight ? -90f : 90f);
            // SoundManager.Instance.PlaySfx(SfxType.Bounce);
        });
    }
    
    public void CancelBudget()
    {
        _slingBudget.Remaining = 0f;
        _graceTimer = 0f;
    }
}
