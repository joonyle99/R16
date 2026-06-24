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
        var simulation = SlingState.Create(origin, dir, _config, fromGround, comboRush);

        while (simulation.Remaining > 0f)
        {
            var evt = SlingSimulator.Tick(ref simulation, _config, _groundLayer, _platformLayer, _enemyLayer, Time.fixedDeltaTime, out Collider2D hitCollider);
            points.Add(simulation.Position);

            if (evt == SlingEvent.Wall)
            {
                if (fromGround || comboRush) break;
                bouncePoints.Add(simulation.Position);
            }
            else if (evt == SlingEvent.Enemy && hitCollider != null && !fromGround)
                hitEnemySet.Add(hitCollider);
        }

        return new SlingResult
        {
            Points = points,
            BouncePoints = bouncePoints,
            BounceCount = bouncePoints.Count,
            TotalDistance = simulation.Total,
            HitEnemies = new List<Collider2D>(hitEnemySet),
        };
    }
}
