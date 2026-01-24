namespace Klacks.Api.Infrastructure.Scripting;

public static class Helper
{
    public static double ExtractDouble(Identifier? value)
    {
        if (value == null) return 0.0;
        return value.Value.AsDouble();
    }

    public static double ExtractDouble(ScriptValue value)
    {
        return value.AsDouble();
    }

    public static int ExtractInt(Identifier? value)
    {
        if (value == null) return 0;
        return value.Value.AsInt();
    }

    public static int ExtractInt(ScriptValue value)
    {
        return value.AsInt();
    }

    public static string ExtractString(Identifier? value)
    {
        if (value == null) return string.Empty;
        return value.Value.AsString();
    }

    public static string ExtractString(ScriptValue value)
    {
        return value.AsString();
    }

    public static bool ExtractBoolean(Identifier? value)
    {
        if (value == null) return false;
        return value.Value.AsBoolean();
    }

    public static bool ExtractBoolean(ScriptValue value)
    {
        return value.AsBoolean();
    }

    public static bool IsNumericInt(string? c)
    {
        return int.TryParse(c, out _);
    }

    public static bool AreEqual(Identifier? left, Identifier? right)
    {
        var leftValue = left?.Value ?? ScriptValue.Null;
        var rightValue = right?.Value ?? ScriptValue.Null;
        return AreEqual(leftValue, rightValue);
    }

    public static bool AreEqual(ScriptValue left, ScriptValue right)
    {
        var leftIsNumeric = left.IsNumber || left.IsBoolean;
        var rightIsNumeric = right.IsNumber || right.IsBoolean;

        if (leftIsNumeric && rightIsNumeric)
        {
            return left.AsDouble() == right.AsDouble();
        }

        return left.AsString().Equals(right.AsString());
    }
}
