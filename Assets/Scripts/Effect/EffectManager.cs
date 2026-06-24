using UnityEngine;
using UnityEngine.Pool;
using JoonyleGameDevKit;
using System.Collections.Generic;

public enum VfxType
{
    Jump_1 = 0,
    Jump_2 = 1,
    Attack = 2,
    ComboRush = 3,

    Dust = 10,
    
    Temp_20 = 20,
    Temp_30 = 30,
    Temp_40 = 40,
}

[System.Serializable]
public struct VfxEntry
{
    public VfxType type;
    public EffectBase prefab;
}

public class EffectManager : Singleton<EffectManager>, IManager
{
    public int Priority => 20;

    [SerializeField] private VfxEntry[] _vfxEntries;

    private Dictionary<VfxType, ObjectPool<EffectBase>> _pools;

    public void Initialize()
    {
        _pools = new Dictionary<VfxType, ObjectPool<EffectBase>>();

        foreach (var entry in _vfxEntries)
        {
            if (entry.prefab == null) continue;
            CreatePool(entry.type, entry.prefab);
        }
    }

    public void Play(VfxType type, Vector2 worldPosition, float rotation = 0f, bool flipX = false, bool flipY = false)
    {
        if (!_pools.TryGetValue(type, out var pool)) return;

        var effect = pool.Get();
        effect.Play(worldPosition, rotation, flipX, flipY);
    }

    private void CreatePool(VfxType type, EffectBase prefab, int defaultCapacity = 10, int maxSize = 300)
    {
        var container = new GameObject($"{type} Container").transform;
        container.SetParent(transform);

        _pools[type] = new ObjectPool<EffectBase>(
            createFunc: () => CreateEffect(type, prefab, container),
            actionOnGet: p => p.gameObject.SetActive(true),
            actionOnRelease: p => p.gameObject.SetActive(false),
            actionOnDestroy: p => { if (p != null) Object.Destroy(p.gameObject); }, // 씬 언로드 시 컨테이너가 먼저 파괴되어 effect가 이미 destroy된 상태일 수 있음
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }

    private EffectBase CreateEffect(VfxType type, EffectBase prefab, Transform container)
    {
        var effect = Object.Instantiate(prefab, container);
        effect.name = type.ToString();
        effect.gameObject.SetActive(false);
        effect.SetReleaseAction(() => _pools[type].Release(effect));
        return effect;
    }
}
