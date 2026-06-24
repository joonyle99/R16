using System;
using UnityEngine;
using System.Collections;

/// <summary>
/// 콤보 러쉬 중 플레이어에게 닿으면 만화 캐릭터처럼 빙글빙글 돌며 화면 밖으로 날아가는 연출.
/// 에디터에서 프리팹에 미리 부착해 사용한다.
/// </summary>
public class RushLaunchable : MonoBehaviour
{
    [Header("Chain")]
    [SerializeField] private RushLaunchable[] _launchWith;

    [Header("Launch")]
    [SerializeField] private float _horizontalSpeed = 40f; // 좌/우로 날아가는 속도
    [SerializeField] private float _upwardSpeed = 7f; // 초기 상승 속도 (포물선)
    [SerializeField] private float _gravity = 22f; // 낙하 가속 (포물선)
    [SerializeField] private float _spinSpeed = 1500f; // 회전 속도 (deg/s)
    [SerializeField] private float _lifetime = 2.5f; // 이 시간 뒤 파괴 (이미 화면 밖)

    private bool _isLaunched;
    public bool IsLaunched => _isLaunched;

    // 발사마다 좌/우를 번갈아 — 만화처럼 의도적으로 양옆으로 흩어진다
    private static int _sideToggle;

    public void Launch() => Launch(NextSide(), null);
    public void Launch(float dirX) => Launch(dirX, null);
    public void Launch(Action onComplete) => Launch(NextSide(), onComplete);

    public void Launch(float dirX, Action onComplete)
    {
        if (_isLaunched) return;
        _isLaunched = true;

        // 더 이상 막거나 부딪히지 않도록 콜라이더/물리를 끈다 (플레이어는 그대로 통과)
        foreach (var col in GetComponentsInChildren<Collider2D>(true)) col.enabled = false;
        var rb = GetComponentInChildren<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        var sign = dirX >= 0f ? 1f : -1f;

        if (_launchWith != null)
            foreach (var other in _launchWith)
            {
                if (other == null) continue;
                
                other.Launch(sign, null);

                // 적 오브젝트라면 게임 로직상 사망 처리 (FSM/Tick 중단) — Launch만으로는 IsDead가 세팅되지 않음
                if (other.TryGetComponent<EnemyBehaviour>(out var enemy) && !enemy.IsDead)
                    enemy.Kill();
            }

        StartCoroutine(LaunchRoutine(sign, onComplete));
    }

    private IEnumerator LaunchRoutine(float dirX, Action onComplete)
    {
        var vy = _upwardSpeed;
        var spin = -dirX * _spinSpeed; // 날아가는 방향으로 굴러가듯 회전
        var elapsed = 0f;

        while (elapsed < _lifetime)
        {
            var dt = Time.deltaTime;
            elapsed += dt;
            vy -= _gravity * dt;
            transform.position += new Vector3(dirX * _horizontalSpeed * dt, vy * dt, 0f);
            transform.Rotate(0f, 0f, spin * dt);
            yield return null;
        }

        onComplete?.Invoke();
        Destroy(gameObject);
    }

    private static float NextSide()
    {
        _sideToggle++;
        return (_sideToggle & 1) == 0 ? 1f : -1f;
    }
}
