using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComboPanel : UIPanel
{
    [SerializeField] private Image _comboGague;
    [SerializeField] private TextMeshProUGUI _comboText;
    [SerializeField] private Sprite _normalGauge;
    [SerializeField] private Sprite _comboRushGauge;

    public void Initialize()
    {

    }

    public void SetComboGague(int currCombo, int targetCombo)
    {
        if (_normalGauge != null && _comboGague.sprite != _normalGauge)
            _comboGague.sprite = _normalGauge;

        _comboGague.fillAmount = targetCombo > 0 ? Mathf.Clamp01((float)currCombo / targetCombo) : 0f;
    }

    public void SetComboRushGague(float remaining, float duration)
    {
        if (_comboRushGauge != null && _comboGague.sprite != _comboRushGauge)
            _comboGague.sprite = _comboRushGauge;

        _comboGague.fillAmount = duration > 0f ? Mathf.Clamp01(remaining / duration) : 0f;
    }

    public void SetComboText(int combo)
    {
        _comboText.text = $"COMBO {combo}";
    }
}
