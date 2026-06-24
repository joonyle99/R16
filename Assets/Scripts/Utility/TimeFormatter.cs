using System;
using System.Text;

public static class TimeFormatter
{
    private static readonly StringBuilder _sb = new(8);

    public static string FormatMMSS(float seconds)
    {
        if (seconds < 0f) seconds = 0f;

        // 카운트다운에서 0.3초 남았으면 "00:01"로 표시되고 0에서 만료되게 ceiling 사용
        var total = (int)MathF.Ceiling(seconds);
        var minutes = total / 60;
        var secs = total % 60;

        _sb.Clear();
        if (minutes < 10) _sb.Append('0');
        _sb.Append(minutes);
        _sb.Append(':');
        if (secs < 10) _sb.Append('0');
        _sb.Append(secs);
        return _sb.ToString();
    }
}
