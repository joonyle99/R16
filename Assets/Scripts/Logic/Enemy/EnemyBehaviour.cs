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

    public void Initialize(PlayerBehaviour player, Action<int> onDamaged, Action onDead)
    {
        InitCombatEntity(onDamaged, onDead);

        _player = player;

        eyeTracker = GetComponentInChildren<EyeTracker>();
        eyeTracker?.Initialize(player.Rigid);

        OnInitialize();
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
