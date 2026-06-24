using TMPro;
using DG.Tweening;
using UnityEngine;

public class ToastPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private float _displayDuration = 1.5f;
    [SerializeField] private float _enterDuration = 0.25f;
    [SerializeField] private float _exitDuration = 0.2f;

    private Sequence _sequence;

    private void OnDestroy() => _sequence?.Kill();

    public void ShowToast(string message)
    {
        _messageText.text = message;

        _sequence?.Kill();
        transform.localScale = Vector3.zero;
        gameObject.SetActive(true);

        _sequence = DOTween.Sequence();
        _sequence.Append(transform.DOScale(Vector3.one, _enterDuration).SetEase(Ease.OutBack));
        _sequence.AppendInterval(_displayDuration);
        _sequence.Append(transform.DOScale(Vector3.zero, _exitDuration).SetEase(Ease.InBack));
        _sequence.OnComplete(() => gameObject.SetActive(false));
    }
}
