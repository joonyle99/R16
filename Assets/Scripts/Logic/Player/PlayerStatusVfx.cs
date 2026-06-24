using UnityEngine;

/// <summary>
/// 일정 시간/상태 동안 플레이어에게 붙어 지속되는 이펙트를 관리한다.
/// 풀링 기반의 일회성 EffectManager와 달리, 플레이어 루트의 자식으로 미리 배치된
/// 루프 이펙트를 SetActive로 켜고 끈다. (루트 자식이라 Visual flip의 영향을 받지 않고 위치만 따라온다)
/// 위치는 루트가 자동으로 끌고 가지만, 회전은 필요 시 Tick에서 속도 방향으로 갱신한다.
/// </summary>
public class PlayerStatusVfx : MonoBehaviour
{
    [SerializeField] private EffectBase _energyVfx;
    [SerializeField] private EffectBase _stunVfx;
    [SerializeField] private EffectBase _propelledVfx;
    [SerializeField] private EffectBase _comboRushVfx;

    private Rigidbody2D _rigid;

    public void Initialize(Rigidbody2D rigid)
    {
        _rigid = rigid;

        HideAll();
    }

    public void Tick(float deltaTime)
    {
        // 추진/러쉬 이펙트는 비행(속도) 방향을 향하도록 회전 갱신
        AlignToVelocity(_propelledVfx);
        AlignToVelocity(_comboRushVfx);
    }

    private void AlignToVelocity(EffectBase vfx)
    {
        if (vfx == null || !vfx.gameObject.activeSelf) return;

        var vel = _rigid.linearVelocity;
        if (vel.sqrMagnitude <= 0.0001f) return;

        var angle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg - 90f;
        vfx.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void ShowEnergy() => Set(_energyVfx, true);
    public void HideEnergy() => Set(_energyVfx, false);
    public void ShowStun() => Set(_stunVfx, true);
    public void HideStun() => Set(_stunVfx, false);
    public void ShowPropelled() => Set(_propelledVfx, true);
    public void HidePropelled() => Set(_propelledVfx, false);
    public void ShowComboRush() => Set(_comboRushVfx, true);
    public void HideComboRush() => Set(_comboRushVfx, false);

    public void HideAll()
    {
        HideEnergy();
        HideStun();
        HidePropelled();
        HideComboRush();
    }

    private static void Set(EffectBase vfx, bool on)
    {
        if (vfx != null && vfx.gameObject.activeSelf != on) vfx.gameObject.SetActive(on);
    }
}
