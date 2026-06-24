# Lessons

## 코딩 스타일

- **정렬용 화이트스페이스 금지**: `=` 등을 맞추기 위한 추가 공백 사용 금지.
  ```csharp
  // ❌
  _levelText.text    = data.Level.ToString();
  _killsText.text    = data.Kills.ToString();

  // ✅
  _levelText.text = data.Level.ToString();
  _killsText.text = data.Kills.ToString();
  ```
