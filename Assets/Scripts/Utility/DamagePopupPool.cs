using UnityEngine;
using UnityEngine.Pool;

public class DamagePopupPool
{
    private const string CONTAINER_NAME = "Damage Popup Container";

    private readonly DamagePopup _prefab;
    private readonly Transform _container;
    private readonly ObjectPool<DamagePopup> _pool;

    public DamagePopupPool(DamagePopup prefab, int defaultCapacity = 50, int maxSize = 500)
    {
        _prefab = prefab;
        var container = GameObject.Find(CONTAINER_NAME);
        if (container == null) container = new GameObject(CONTAINER_NAME);
        _container = container.transform;
        _pool = new ObjectPool<DamagePopup>(
            createFunc: CreatePopup,
            actionOnGet: p => p.gameObject.SetActive(true),
            actionOnRelease: p => p.gameObject.SetActive(false),
            actionOnDestroy: p => { if (p != null) Object.Destroy(p.gameObject); }, // 씬 언로드 시 컨테이너가 먼저 파괴되어 popup이 이미 destroy된 상태일 수 있음
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }

    private DamagePopup CreatePopup()
    {
        var popup = Object.Instantiate(_prefab, _container);
        popup.Initialize();
        popup.SetReleaseAction(() => _pool.Release(popup));
        popup.gameObject.SetActive(false);
        return popup;
    }

    public void Spawn(int amount, Vector3 worldPos)
    {
        var popup = _pool.Get();
        popup.Play(amount, worldPos);
    }

    public void Clear() => _pool.Clear();
}
