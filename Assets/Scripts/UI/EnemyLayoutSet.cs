using UnityEngine;

public class EnemyLayoutSet : MonoBehaviour
{
    public EnemyIconSlot[] EnemyIconSlots { get; private set; }

    public void Initialize()
    {
        EnemyIconSlots = GetComponentsInChildren<EnemyIconSlot>();
        foreach (var enemyIconSlot in EnemyIconSlots)
        {
            enemyIconSlot.Initialize();
        }
    }
}
