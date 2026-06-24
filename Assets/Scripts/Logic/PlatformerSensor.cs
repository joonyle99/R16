using UnityEngine;

public class PlatformerSensor : MonoBehaviour
{
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _platformLayer;

    public LayerMask GroundLayer => _groundLayer;
    public LayerMask PlatformLayer => _platformLayer;
    public LayerMask GroundAndPlatform => _groundLayer | _platformLayer;

    public bool IsGrounded { get; private set; }

    // 이 값 초과로 상승 중일 때만 착지 불가 처리 (디펜트레이션 노이즈 흡수)
    private const float RISING_VELOCITY_THRESHOLD = 0.5f;
    // 접촉 법선의 y가 이 값 이상이면 바닥으로 분류 (≈ 60° 이내 경사)
    private const float GROUND_NORMAL_MIN_Y = 0.5f;

    private Rigidbody2D _rigid;
    private ContactFilter2D _contactFilter;
    private readonly ContactPoint2D[] _contacts = new ContactPoint2D[8];

    public void Initialize()
    {
        _rigid = GetComponent<Rigidbody2D>();
        _contactFilter = new ContactFilter2D();
        _contactFilter.SetLayerMask(GroundAndPlatform);
    }

    public void FixedTick(float fixedDeltaTime)
    {
        // 상승 중이면 착지 판정 불가
        // (발사 직후 1틱은 직전 스텝의 바닥 접촉이 아직 남아 있어, 이를 착지로 오인하면 발사가 즉시 취소된다)
        if (_rigid.linearVelocity.y > RISING_VELOCITY_THRESHOLD)
        {
            IsGrounded = false;
            return;
        }

        // 캐스트가 아니라 물리 솔버가 실제로 만든 접촉으로 판정한다.
        // Platform Effector가 통과시킨(disable한) 접촉은 enabled == false라서
        // one-way 플랫폼 내부에 겹쳐 있어도 착지로 오인하지 않는다.
        IsGrounded = false;
        var count = _rigid.GetContacts(_contactFilter, _contacts);
        for (int i = 0; i < count; i++)
        {
            var contact = _contacts[i];
            if (contact.enabled && contact.normal.y >= GROUND_NORMAL_MIN_Y)
            {
                IsGrounded = true;
                return;
            }
        }
    }
}
