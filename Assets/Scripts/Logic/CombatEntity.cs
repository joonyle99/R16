using System;
using UnityEngine;

public abstract class CombatEntity : MonoBehaviour
{
    public Animator Animator { get; protected set; }
    public Transform Visual { get; protected set; }
    public Transform Pivot { get; protected set; }
    public SpriteRenderer SpriteRenderer { get; protected set; }
    protected Material Material { get; set; }
    public Rigidbody2D Rigid { get; protected set; }

    [SerializeField] protected int maxHp;
    public int MaxHp => maxHp;

    protected int currHp;
    public int CurrHp => currHp;

    public bool IsDead => currHp <= 0;
    public bool IsInvincible { get; set; }

    private Action<int> _onDamaged;
    private Action _onDead;

    protected bool isFacingRight;

    protected void InitCombatEntity(Action<int> onDamaged, Action onDead)
    {
        _onDamaged = onDamaged;
        _onDead = onDead;

        Animator = GetComponentInChildren<Animator>();
        Visual = transform.Find("Visual");
        Pivot = Visual.Find("Pivot");
        SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        Material = SpriteRenderer.material;
        Rigid = GetComponentInChildren<Rigidbody2D>();

        currHp = maxHp;
        isFacingRight = true;
    }

    public virtual void FixedTick(float deltaTime) { }

    public virtual void Tick(float deltaTime) { }

    // ========= ... =========

    public virtual void TakeDamage(int damage, Vector2 sourcePos)
    {
        if (IsDead || IsInvincible) return;
        currHp -= damage;
        OnDamaged(damage, sourcePos);
        if (IsDead) OnDead();
    }

    public virtual void Kill()
    {
        if (IsDead) return;
        currHp = 0;
        OnDead();
    }

    // ========= ... =========

    protected virtual void OnDamaged(int damage, Vector2 sourcePos)
    {
        _onDamaged?.Invoke(damage);
    }

    protected virtual void OnDead()
    {
        _onDead?.Invoke();
    }

    // ========= ... =========

    public void PlayAnimation(int stateHash, float crossFade = 0f) => Animator.CrossFade(stateHash, crossFade);
    public void PlayAnimation(string stateName, float crossFade = 0f) => Animator.CrossFade(stateName, crossFade);
    public void SetFacingDir(bool facingRight)
    {
        isFacingRight = facingRight;

        // 물리 루트의 스케일을 뒤집으면 콜라이더가 재생성되어 접촉이 한 틱 끊기므로 비주얼만 뒤집는다
        var scale = Visual.localScale;
        scale.x = Mathf.Abs(scale.x) * (isFacingRight ? 1f : -1f);
        Visual.localScale = scale;
    }
}
