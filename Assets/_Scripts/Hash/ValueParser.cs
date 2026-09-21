using System;
using System.Globalization;

public enum ValueType { Int, Float, String }

public static class ValueParser
{
    // 입력 문자열을 선택한 실제 타입으로 변환하며 실패 시 false를 반환한다.
    public static bool TryParse(string text, ValueType type, out object value)
    {
        value = null;
        switch (type)
        {
            case ValueType.Int:
                if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int integer))
                    value = integer;
                break;
            case ValueType.Float:
                // 입력 필드가 허용하는 소수 구분자를 통일하고 NaN과 무한대는 거부한다.
                if (float.TryParse(text?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float number)
                    && !float.IsNaN(number) && !float.IsInfinity(number))
                    value = number;
                break;
            case ValueType.String:
                value = text ?? string.Empty;
                break;
        }
        return value != null;
    }

    // 문자열은 따옴표로 구분하고 숫자는 지역 설정에 관계없이 같은 형식으로 표시한다.
    public static string Format(object value)
    {
        if (value is string text) return "\"" + text + "\"";
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }
}

