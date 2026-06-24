using TMPro;
using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 상단 미션 UI. 요구 몬스터 수에 맞는 레이아웃을 활성화하고 슬롯에 아이콘을 할당한다.
/// 각 레이아웃(1~9개)은 에디터에서 미리 배치한다.
/// </summary>
public class MissionPanel : UIPanel
{
    [Serializable]
    private struct EnemyTypeVisual
    {
        public EnemyType type;
        public Sprite icon;
    }

    [SerializeField] private EnemyTypeVisual[] _enemyTypeVisuals;
    [SerializeField] private EnemyLayoutSet[] _enemyLayoutSets;
    [SerializeField] private Image _rushEffect;
    [SerializeField] private Image _background;
    [SerializeField] private Image _timer;
    [SerializeField] private GameObject _missionContent;
    [SerializeField] private TextMeshProUGUI _chainText;
    [SerializeField] private float _shakeStrength = 16f;
    [SerializeField] private float _shakeDuration = 0.3f;

    private RectTransform _rect;
    private MissionDefinition _mission;
    private EnemyLayoutSet _activeLayout;

    public void Initialize()
    {
        _rect = (RectTransform)transform;

        foreach (var layout in _enemyLayoutSets)
        {
            layout.gameObject.SetActive(false);
            layout.Initialize();
        }
    }

    public void SetMission(MissionDefinition mission)
    {
        _mission = mission;

        // 처치해야 할 적 1마리 = 슬롯 1개. required 수만큼 반복 추가해 총 슬롯 수를 결정한다.
        var icons = new List<(EnemyType type, Sprite icon)>();

        // 슬롯 배치 순서를 고정
        // 타입 순서대로 아이콘 목록 구성
        // mission.Requirements는 딕셔너리라 순서가 보장되지 않음
        foreach (var visual in _enemyTypeVisuals)
        {
            if (!mission.Requirements.TryGetValue(visual.type, out var required) || required <= 0) continue;

            for (int i = 0; i < required; i++)
                icons.Add((visual.type, visual.icon));
        }

        // 이전 레이아웃 비활성화
        if (_activeLayout != null) _activeLayout.gameObject.SetActive(false);
        _activeLayout = null;

        // 슬롯 수에 맞는 레이아웃 탐색 (선형 탐색)
        foreach (var layout in _enemyLayoutSets)
        {
            if (layout != null && layout.EnemyIconSlots != null
                && layout.EnemyIconSlots.Length == icons.Count)
            {
                _activeLayout = layout;
                
                break;
            }
        }

        if (_activeLayout == null)
        {
            Debug.LogWarning($"[MissionPanel] {icons.Count}개짜리 레이아웃 없음");
            return;
        }

        _activeLayout.gameObject.SetActive(true);

        var slots = _activeLayout.EnemyIconSlots;
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = _activeLayout.EnemyIconSlots[i];
            slot.Setup(icons[i].icon);
            slot.SetActiveOverlay(false);
        }

        SetVisible(true);
    }

    public void SetProgress(EnemyType type, int killed)
    {
        if (_activeLayout == null) return;

        // 타입 순서대로 슬롯을 찾아 killed 수만큼 앞에서부터 표시
        // 슬롯 순서는 SetMission에서 _typeVisuals 순서로 채웠으므로 동일하게 순회

        int count = 0;
        int slotIdx = 0;

        foreach (var visual in _enemyTypeVisuals)
        {
            if (!_mission.Requirements.TryGetValue(visual.type, out var required) || required <= 0) continue;

            for (int i = 0; i < required && slotIdx < _activeLayout.EnemyIconSlots.Length; i++, slotIdx++)
            {
                if (visual.type == type) _activeLayout.EnemyIconSlots[slotIdx].SetActiveOverlay(count++ < killed);
            }
        }
    }

    public void ResetProgress()
    {
        if (_activeLayout == null) return;

        foreach (var slot in _activeLayout.EnemyIconSlots)
            slot.SetActiveOverlay(false);

        PlayShatter();
    }

    public void SetTimer(float remaining, float limit)
    {
        if (_timer != null) _timer.fillAmount = limit > 0f ? Mathf.Clamp01(remaining / limit) : 0f;
    }

    public void SetChainProgress(int curr, int max)
    {
        if (_chainText != null) _chainText.text = $"{curr}/{max}";
    }

    public void SetRushActive(bool active)
    {
        if (_missionContent != null) _missionContent.SetActive(!active);
        if (_rushEffect != null) _rushEffect.gameObject.SetActive(active);
    }

    private void PlayShatter()
    {
        if (_rect == null) return;
        
        _rect.DOComplete();
        _rect.DOShakeAnchorPos(_shakeDuration, _shakeStrength, 18, 90, false, true);
    }
}
