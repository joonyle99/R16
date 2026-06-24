using UnityEngine;

public sealed class ComboRushHitbox : MonoBehaviour
{
    private PlayerBehaviour _player;

    public void Initialize(PlayerBehaviour player)
    {
        _player = player;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        _player?.CheckComboRushHit(collider);
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        _player?.CheckComboRushHit(collider);
    }
}
