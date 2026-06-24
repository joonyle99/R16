using UnityEngine;

public enum SlingEvent
{
    None,
    Wall,    // 벽 반사 성공
    NonWall, // 바닥·천장 충돌 → 궤적 종료
    Enemy,   // 적 감지 (통과)
}

public struct SlingState
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Remaining;
    public float Total; // 누적 이동 거리
    public int Bounces;

    public static SlingState Create(Vector2 origin, Vector2 dir, SlingConfig config, bool fromGround, bool comboRush = false)
    {
        var speed = comboRush ? config.comboRushSlingSpeed
                  : fromGround ? config.groundSlingSpeed
                  : config.airSlingSpeed;
        return new SlingState
        {
            Position = origin,
            Velocity = dir * speed,
            Remaining = config.slingTime,
        };
    }
}

public static class SlingSimulator
{
    public static SlingEvent Tick(ref SlingState state, SlingConfig config, LayerMask groundLayer, LayerMask platformLayer, LayerMask enemyLayer, float deltaTime, out Collider2D hitEnemyCollider)
    {
        hitEnemyCollider = null;

        state.Velocity.y -= config.slingGravity * deltaTime;
        state.Remaining -= deltaTime;

        var step = state.Velocity * deltaTime;
        var stepDist = step.magnitude;
        var allHits = Physics2D.CircleCastAll(
                state.Position,
                config.circleCastRadius,
                step.normalized,
                stepDist,
                groundLayer | platformLayer | enemyLayer
            );

        bool hitEnemy = false;
        foreach (var hit in allHits)
        {
            if (hit.distance <= 0f) continue;

            // enemyLayer: 통과, 감지만
            if ((enemyLayer.value & (1 << hit.collider.gameObject.layer)) != 0)
            {
                if (hitEnemyCollider == null) hitEnemyCollider = hit.collider; // 이 틱에서 가장 가까운 적
                hitEnemy = true;
                continue;
            }

            // platformLayer: 윗면 차단, 밑면 / 옆면 통과 (원웨이 판정)
            if ((platformLayer.value & (1 << hit.collider.gameObject.layer)) != 0)
            {
                // 윗면 차단
                if (hit.normal.y > 0f)
                {
                    state.Total += hit.distance;
                    state.Position = hit.centroid;
                    state.Remaining = 0f;
                    return SlingEvent.NonWall;
                }

                // 밑면 / 옆면 통과
                continue;
            }

            // groundLayer: 옆면 벽 킥, 윗면 / 아랫면 차단
            state.Total += hit.distance;
            state.Position = hit.centroid;

            if (!IsWall(hit.normal, config))
            {
                state.Remaining = 0f;
                return SlingEvent.NonWall;
            }

            if (CanBounce(in state, config))
            {
                Bounce(ref state, hit.normal, config);
                return SlingEvent.Wall;
            }

            return SlingEvent.None;
        }

        state.Position += step;
        state.Total += stepDist;
        return hitEnemy ? SlingEvent.Enemy : SlingEvent.None;
    }

    // 법선이 수평에 가까우면 벽 (바닥·천장 모두 NonWall로 분류)
    public static bool IsWall(Vector2 normal, SlingConfig config)
        => Vector2.Angle(normal, Vector2.up) > config.wallAngleThreshold
        && Vector2.Angle(normal, Vector2.down) > config.wallAngleThreshold;

    public static bool CanBounce(in SlingState state, SlingConfig config)
        => state.Total >= config.minDistance && state.Remaining > 0f && state.Bounces < config.maxBounces;

    public static void Bounce(ref SlingState state, Vector2 normal, SlingConfig config)
    {
        state.Bounces++;
        state.Remaining += config.bonusTime;
        state.Velocity = Kick(normal, config.kick);
    }

    // 땅샷: 조준 고도각(수평 기준, 아래쪽은 음수)이 임계값 이하면 포물선 대신 직선 발사로 취급
    public static bool IsGroundShot(Vector2 dir, SlingConfig config)
        => Mathf.Atan2(dir.y, Mathf.Abs(dir.x)) * Mathf.Rad2Deg <= config.groundShotMaxAngle;

    public static Vector2 Kick(Vector2 normal, SlingConfig.KickConfig kick)
    {
        var rad = kick.angle * Mathf.Deg2Rad;
        var away = Mathf.Sign(normal.x);
        return new Vector2(Mathf.Cos(rad) * away, Mathf.Sin(rad)) * kick.speed;
    }
}
