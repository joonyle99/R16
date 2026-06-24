using System;
using System.Text;

/// <summary>
/// 방어 복사(defensive copy) 생략을 위해 readonly struct 사용
/// struct에 붙이면 → 모든 필드가 불변이라는 걸 컴파일러에게 보장 (방어 복사 생략)
/// </summary>
public readonly struct NumberSuffix
{
    // 변수의 경우
    // const - 컴파일 초기화
    // readonly - 런타임 초기화 (단 생성자에서 초기화 가능)
    // 필드에 붙이면 → 그 필드 하나만 생성 후 변경 불가

    public readonly int Threshold;
    public readonly char Symbol;

    public NumberSuffix(int threshold, char symbol)
    {
        Threshold = threshold;
        Symbol = symbol;
    }
}

public static class NumberFormatter
{
    // static readonly라서 앱 시작 시 딱 1번만 할당
    private static readonly StringBuilder _sb = new(8);
    private static readonly NumberSuffix[] _numberSuffixes =
    {
        // struct의 new는 스택에 할당된다 (힙 할당 X)
        new(1_000_000_000, 'B'),
        new(1_000_000, 'M'),
        new(1_000, 'K'),
    };

    public static string FormatDamage(int value)
    {
        foreach (var suffix in _numberSuffixes)
        {
            if (value >= suffix.Threshold)
            {
                var rounded = MathF.Round(value / (float)suffix.Threshold, 1);
                _sb.Clear();
                _sb.Append(rounded);
                _sb.Append(suffix.Symbol);
                return _sb.ToString();
            }
        }
        
        return value.ToString();
    }
}
