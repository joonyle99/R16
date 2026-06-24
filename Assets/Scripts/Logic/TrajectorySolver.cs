using System;
using UnityEngine;
using System.Collections.Generic;

public struct SlingResult
{
    public List<Vector2> Points; // 선 렌더러용 전체 점
    public List<Vector2> BouncePoints;
    public int BounceCount;
    public float TotalDistance;
    public List<Collider2D> HitEnemies;
    public bool HitEnemy => HitEnemies != null && HitEnemies.Count > 0;
    public bool HitInteractableButton; // 궤적이 InteractableButton에 닿는지 (조준선 강조용)
}

public class TrajectorySolver
{
    private readonly SlingConfig _config;
    private readonly LayerMask _groundLayer;
    private readonly LayerMask _platformLayer;
    private readonly LayerMask _enemyLayer;

    public TrajectorySolver(SlingConfig config, LayerMask groundLayer, LayerMask platformLayer, LayerMask enemyLayer)
    {
        _config = config;
        _groundLayer = groundLayer;
        _platformLayer = platformLayer;
        _enemyLayer = enemyLayer;
    }

    public SlingResult Solve(Vector2 origin, Vector2 dir, bool fromGround, bool comboRush = false)
    {
        var points = new List<Vector2> { origin };
        var bouncePoints = new List<Vector2>();
        var hitEnemySet = new HashSet<Collider2D>();
        var hitInteractableButton = false;
        var simulation = SlingState.Create(origin, dir, _config, fromGround, comboRush);

        while (simulation.Remaining > 0f)
        {
            var evt = SlingSimulator.Tick(ref simulation, _config, _groundLayer, _platformLayer, _enemyLayer, Time.fixedDeltaTime, out Collider2D hitEnemyCollider, out Collider2D hitSolidCollider);
            points.Add(simulation.Position);

            // 벽킥으로 break하기 전에 먼저 판별 (버튼을 벽으로 맞고 멈춰도 강조되도록)
            // 적 락온과 동일하게 공중 조준(!fromGround)일 때만 강조한다
            if (!fromGround && hitSolidCollider != null && hitSolidCollider.GetComponent<InteractableButton>() != null)
                hitInteractableButton = true;

            if (evt == SlingEvent.Wall)
            {
                if (fromGround || comboRush) break;
                bouncePoints.Add(simulation.Position);
            }
            else if (evt == SlingEvent.Enemy && hitEnemyCollider != null && !fromGround)
                hitEnemySet.Add(hitEnemyCollider);
        }

        return new SlingResult
        {
            Points = points,
            BouncePoints = bouncePoints,
            BounceCount = bouncePoints.Count,
            TotalDistance = simulation.Total,
            HitEnemies = new List<Collider2D>(hitEnemySet),
            HitInteractableButton = hitInteractableButton,
        };
    }
}
