using System;
using UnityEngine;
using System.Collections.Generic;

public class SlingBehaviour : MonoBehaviour
{
    [SerializeField] private SlingConfig _config;
    public SlingConfig Config => _config;

    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private Color _defaultColor;
    [SerializeField] private Color _targetColor;

    [SerializeField] private SpriteRenderer _bounceMarkerPrefab;
    [SerializeField] private SpriteRenderer _arrowMarkerPrefab;
    [SerializeField] private SpriteRenderer _lockOnMarkerPrefab;
    [SerializeField] private int _maxLockOnMarkers = 4;

    private Rigidbody2D _rigid;
    public Rigidbody2D Rigid => _rigid;
    private TrajectorySolver _solver;
    private LineRenderer _line;

    private bool _isActiveSling;
    public bool IsActiveSling => _isActiveSling;

    private bool _isPendingShot; // Shoot 직후 한 번만 true — AirState가 "발사 진입"인지 "그냥 낙하"인지 구분하는 용도
    public Vector2 LastShotDir { get; private set; }
    public bool LastShotFromGround { get; private set; }

    // 최대 차지는 런타임 상태 — config의 baseCharges로 시작, 업그레이드로 증가 (SO에는 다시 쓰지 않는다)
    public int TotalCharges { get; private set; }
    public int CurrCharges { get; private set; }
    public bool HasCharge => CurrCharges > 0;
    public event Action<int, int> OnChargesChanged; // (curr, max)
    public event Action OnShootSling;

    private LineRenderer[] _segmentLines; // 바운스마다 LineRenderer를 분리해 miter join 왜곡을 방지
    private SpriteRenderer[] _bounceMarkers;
    private SpriteRenderer _arrowMarker;
    private SpriteRenderer[] _lockOnMarkers;
    private readonly HashSet<Collider2D> _prevLockOnSet = new(); // 직전 프레임 락온된 적 — 새 적 진입(rising edge) 감지용

    public void Initialize(Rigidbody2D rigid, LayerMask groundLayer, LayerMask platformLayer)
    {
        _rigid = rigid;

        _solver = new TrajectorySolver(_config, groundLayer, platformLayer, _enemyLayer);

        _line = GetComponentInChildren<LineRenderer>();
        _line.textureMode = LineTextureMode.Tile;
        _line.useWorldSpace = true;
        _line.enabled = false;

        // 세그먼트 LineRenderer 풀: 바운스 수만큼 추가 세그먼트 필요 (총 maxBounces + 1개)
        _segmentLines = new LineRenderer[_config.maxBounces + 1];
        _segmentLines[0] = _line;
        for (int i = 1; i < _segmentLines.Length; i++)
        {
            var go = new GameObject($"Aim Line{i}");
            go.transform.SetParent(transform, false);
            go.layer = _line.gameObject.layer;
            var lr = go.AddComponent<LineRenderer>();
            lr.textureMode = _line.textureMode;
            lr.textureScale = _line.textureScale;
            lr.useWorldSpace = _line.useWorldSpace;
            lr.sharedMaterial = _line.sharedMaterial;
            lr.widthCurve = _line.widthCurve;
            lr.widthMultiplier = _line.widthMultiplier;
            lr.colorGradient = _line.colorGradient;
            lr.shadowCastingMode = _line.shadowCastingMode;
            lr.sortingLayerID = _line.sortingLayerID;
            lr.sortingOrder = _line.sortingOrder;
            lr.enabled = false;
            _segmentLines[i] = lr;
        }

        _bounceMarkers = new SpriteRenderer[_config.maxBounces];
        for (int i = 0; i < _bounceMarkers.Length; i++)
        {
            _bounceMarkers[i] = Instantiate(_bounceMarkerPrefab, transform);
            _bounceMarkers[i].enabled = false;
        }

        _arrowMarker = Instantiate(_arrowMarkerPrefab, transform);
        _arrowMarker.enabled = false;

        _lockOnMarkers = new SpriteRenderer[_maxLockOnMarkers];
        for (int i = 0; i < _lockOnMarkers.Length; i++)
        {
            _lockOnMarkers[i] = Instantiate(_lockOnMarkerPrefab, transform);
            _lockOnMarkers[i].enabled = false;
        }

        TotalCharges = _config.baseCharges;
        CurrCharges = TotalCharges;
    }

    public void SetActiveSling(bool active) => _isActiveSling = active;

    // ============ ... ============

    public void ShowTrajectory(Vector2 dragOffset, bool fromGround, bool comboRush = false)
    {
        var slingDir = (-1) * dragOffset.normalized;

        // 조준선 원점은 물리 위치(_rigid.position)가 아니라 보간된 렌더 위치를 쓴다.
        // 물리 위치는 물리 스텝에서만 갱신돼서, 슬로우(aimTimeScale) 중 낙하하며 조준하면 선이 한 박자 늦게 따라온다.
        var origin = (Vector2)_rigid.transform.position;

        // 잠시 주석처리
        // if (SlingSimulator.IsGroundShot(slingDir, _config))
        // {
        //     ShowGroundShotLine(slingDir);
        //     return;
        // }

        var slingResult = _solver.Solve(origin, slingDir, fromGround, comboRush);

        if (slingResult.TotalDistance < _config.minArrowDistance)
        {
            HideTrajectory();
            return;
        }

        var aimColor = slingResult.HitEnemy ? _targetColor : _defaultColor;

        // 바운스 지점을 기준으로 Points를 구간별로 나눠 별도 LineRenderer에 할당
        // → 구간 간 miter join이 생기지 않아 꺾임 부분 텍스처 왜곡이 사라짐
        {
            var allPoints = slingResult.Points;
            var bounceSet = new HashSet<Vector2>(slingResult.BouncePoints);

            int segIdx = 0;
            int start = 0;

            for (int i = 0; i <= allPoints.Count; i++)
            {
                bool isBounce = i < allPoints.Count && bounceSet.Contains(allPoints[i]) && i != 0;
                bool isEnd = i == allPoints.Count;

                if ((isBounce || isEnd) && segIdx < _segmentLines.Length)
                {
                    int count = i - start + (isBounce ? 1 : 0); // 바운스 점은 이 세그먼트에 포함
                    var lr = _segmentLines[segIdx];
                    lr.positionCount = count;
                    for (int j = 0; j < count; j++)
                        lr.SetPosition(j, allPoints[start + j]);
                    lr.startColor = aimColor;
                    lr.endColor = aimColor;
                    lr.enabled = true;

                    if (isBounce)
                        start = i; // 다음 세그먼트는 바운스 점부터 시작 (공유)
                    segIdx++;
                }
            }

            // 남은 세그먼트 비활성화
            for (int i = segIdx; i < _segmentLines.Length; i++)
            {
                _segmentLines[i].positionCount = 0;
                _segmentLines[i].enabled = false;
            }
        }

        {
            for (int i = 0; i < _bounceMarkers.Length; i++)
            {
                if (i < slingResult.BouncePoints.Count)
                {
                    _bounceMarkers[i].transform.position = slingResult.BouncePoints[i];
                    _bounceMarkers[i].color = aimColor;
                    _bounceMarkers[i].enabled = true;
                }
                else
                {
                    _bounceMarkers[i].enabled = false;
                }
            }
        }

        {
            var allPoints = slingResult.Points;
            if (allPoints.Count >= 2)
            {
                var tip = allPoints[allPoints.Count - 1];
                var prev = allPoints[allPoints.Count - 2];
                var dir = (tip - prev).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                _arrowMarker.transform.position = tip;
                _arrowMarker.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                _arrowMarker.color = aimColor;
                _arrowMarker.enabled = true;
            }
        }

        {
            var hitEnemies = slingResult.HitEnemies;

            int count = Mathf.Min(hitEnemies.Count, _lockOnMarkers.Length);

            bool newLockOn = false; // 개수가 아니라 정체로 비교 — 한 마리가 빠지고 다른 적이 들어와도(1→1) 재생되게

            for (int i = 0; i < count; i++)
            {
                if (!_prevLockOnSet.Contains(hitEnemies[i])) { newLockOn = true; break; }
            }
            
            if (newLockOn)
            {
                SoundManager.Instance.PlaySfx(SfxType.LockOn);
            }

            _prevLockOnSet.Clear();
            
            for (int i = 0; i < count; i++)
            {
                _prevLockOnSet.Add(hitEnemies[i]);
            }

            for (int i = 0; i < _lockOnMarkers.Length; i++)
            {
                if (i < count)
                {
                    _lockOnMarkers[i].transform.position = hitEnemies[i].transform.position;
                    _lockOnMarkers[i].enabled = true;
                }
                else
                {
                    _lockOnMarkers[i].enabled = false;
                }
            }
        }
    }

    // 땅샷 조준선: 포물선 대신 짧은 고정 길이 직선
    private void ShowGroundShotLine(Vector2 slingDir)
    {
        var origin = (Vector2)_rigid.transform.position;

        _line.positionCount = 2;
        _line.SetPosition(0, origin);
        _line.SetPosition(1, origin + slingDir * _config.groundShotAimLineLength);
        _line.enabled = true;

        foreach (var marker in _bounceMarkers)
            marker.enabled = false;
    }

    public void HideTrajectory()
    {
        foreach (var lr in _segmentLines)
            lr.enabled = false;
        foreach (var marker in _bounceMarkers)
            marker.enabled = false;
        _arrowMarker.enabled = false;
        foreach (var marker in _lockOnMarkers)
            marker.enabled = false;
        _prevLockOnSet.Clear(); // 조준 종료 → 다음 조준에서 처음부터 다시 감지
    }

    public void ShootSling(Vector2 dragOffset, bool consumeCharge, bool fromGround = false)
    {
        _isPendingShot = true;

        SoundManager.Instance.PlaySfx(SfxType.Shoot);
        var shotDir = (-1) * dragOffset.normalized;
        LastShotDir = shotDir;
        LastShotFromGround = fromGround;

        if (consumeCharge)
        {
            CurrCharges = Mathf.Max(0, CurrCharges - 1);
            OnChargesChanged?.Invoke(CurrCharges, TotalCharges);
        }

        OnShootSling?.Invoke();
    }

    public void RestoreCharges()
    {
        if (CurrCharges == TotalCharges) return;

        CurrCharges = TotalCharges;
        OnChargesChanged?.Invoke(CurrCharges, TotalCharges);
    }

    public void AddCharge(int amount = 1)
    {
        if (CurrCharges >= TotalCharges) return;

        CurrCharges = Mathf.Min(TotalCharges, CurrCharges + amount);
        OnChargesChanged?.Invoke(CurrCharges, TotalCharges);
    }

    // 최대 차지 업그레이드: 늘어난 칸은 즉시 채워서 획득이 바로 체감되게 한다
    public void IncreaseTotalCharges(int amount = 1)
    {
        TotalCharges += amount;
        CurrCharges = Mathf.Min(CurrCharges + amount, TotalCharges);
        OnChargesChanged?.Invoke(CurrCharges, TotalCharges);
    }

    public bool ConsumeSling()
    {
        if (!_isPendingShot) return false;
        _isPendingShot = false;
        return true;
    }
}
