using System;
using UnityEngine;
using JoonyleGameDevKit;
using System.Collections;

/// <summary>처치 미션에서 적을 타입별로 집계하기 위한 식별자.</summary>
public enum EnemyType
{
    Flying,
    Floating,
    Digging,
    Rotating,
}

public abstract class EnemyBehaviour : CombatEntity
{
    [SerializeField] private float _hitInvincibleDuration = 0.1f;

    public abstract EnemyType EnemyType { get; }

    private PlayerBehaviour _player;
    public PlayerBehaviour Player => _player;

    protected EyeTracker eyeTracker;

    // 미션 대상 강조용 외곽선: MissionOutlineRendererFeature 가 잡는 Rendering Layer 비트를 자식 SpriteRenderer 들에 토글한다.
    // GetMask 는 생성자/필드 초기화에서 호출 불가(Unity 제약) → 런타임에 한 번만 지연 계산해 캐시한다.
    private static uint _missionOutlineMask;
    private static bool _missionOutlineMaskResolved;
    private SpriteRenderer[] _spriteRenderers;
    private uint[] _baseRenderingLayerMasks; // 토글 후 원래 마스크로 되돌리기 위한 보존값

    public void Initialize(PlayerBehaviour player, Action<int> onDamaged, Action onDead)
    {
        InitCombatEntity(onDamaged, onDead);

        _player = player;

        eyeTracker = GetComponentInChildren<EyeTracker>();
        eyeTracker?.Initialize(player.Rigid);

        CacheSpriteRenderers();

        OnInitialize();
    }

    private void CacheSpriteRenderers()
    {
        if (!_missionOutlineMaskResolved)
        {
            _missionOutlineMask = RenderingLayerMask.GetMask("MissionOutline");
            _missionOutlineMaskResolved = true;
        }

        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        _baseRenderingLayerMasks = new uint[_spriteRenderers.Length];
        for (var i = 0; i < _spriteRenderers.Length; i++)
            _baseRenderingLayerMasks[i] = _spriteRenderers[i].renderingLayerMask;
    }

    /// <summary>미션 대상 강조 외곽선 on/off. 자식 SpriteRenderer 들의 Rendering Layer 비트만 토글하고 원래 마스크는 보존한다.</summary>
    public void SetMissionHighlight(bool on)
    {
        if (_spriteRenderers == null) return;

        for (var i = 0; i < _spriteRenderers.Length; i++)
        {
            var renderer = _spriteRenderers[i];
            if (renderer == null) continue;

            var baseMask = _baseRenderingLayerMasks[i];
            renderer.renderingLayerMask = on ? (baseMask | _missionOutlineMask) : (baseMask & ~_missionOutlineMask);
        }
    }

    public override void FixedTick(float deltaTime)
    {
        if (IsDead) return;

        base.FixedTick(deltaTime);
    }

    public override void Tick(float deltaTime)
    {
        if (IsDead) return;

        base.Tick(deltaTime);

        eyeTracker?.Tick(deltaTime);
    }

    // ============ ... ============

    protected abstract void OnInitialize();

    protected override void OnDamaged(int damage, Vector2 sourcePos)
    {
        base.OnDamaged(damage, sourcePos);
        StartCoroutine(HitInvincibleRoutine());
    }
    
    protected override void OnDead()
    {
        base.OnDead();

        if (TryGetComponent<RushLaunchable>(out var rushLaunchable) && rushLaunchable.IsLaunched) return; // RushLaunchable이 LaunchRoutine 끝에서 Destroy 처리
        // SpriteExploder.Explode(SpriteRenderer, transform.position);
        Destroy(gameObject);
    }

    private IEnumerator HitInvincibleRoutine()
    {
        IsInvincible = true;
        yield return new WaitForSeconds(_hitInvincibleDuration);
        IsInvincible = false;
    }
}
