using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResultPanel : UIPanel
{
    [SerializeField] private TextMeshProUGUI _killsText;
    [SerializeField] private TextMeshProUGUI _maxComboText;
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private Button _button;

    private void OnDestroy()
    {
        _button.onClick.RemoveAllListeners();
    }

    public void Initialize()
    {
        _button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        SceneManager.LoadScene("OutGameScene");
    }

    public void Show(GameResultData data)
    {
        _killsText.text = $"킬: {data.Kills}";
        _maxComboText.text = $"최대 콤보: {data.MaxCombo}";
        _timeText.text = $"시간: {TimeFormatter.FormatMMSS(data.Time)}";
        
        SetVisible(true);
    }
}
