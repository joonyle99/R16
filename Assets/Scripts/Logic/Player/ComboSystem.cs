using System;
using UnityEngine;

public class ComboSystem
{
    public int Count { get; private set; }
    public int MaxCount { get; private set; }

    public event Action<int, int> OnComboChanged;
    public event Action OnComboRushThresholdReached;

    private int _comboRushThreshold;
    private bool _comboRushTriggered;

    public void Initialize(int comboRushThreshold)
    {
        _comboRushThreshold = comboRushThreshold;
        _comboRushTriggered = false;
    }

    public void Add()
    {
        Count++;
        if (Count > MaxCount) MaxCount = Count;
        OnComboChanged?.Invoke(Count, _comboRushThreshold);

        if (!_comboRushTriggered && Count >= _comboRushThreshold)
        {
            _comboRushTriggered = true;
            OnComboRushThresholdReached?.Invoke();
        }
    }

    public void Reset()
    {
        Count = 0;
        _comboRushTriggered = false;
        OnComboChanged?.Invoke(Count, _comboRushThreshold);
    }
}
