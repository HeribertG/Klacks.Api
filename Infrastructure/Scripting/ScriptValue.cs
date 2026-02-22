// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Globalization;
using System.Runtime.InteropServices;

namespace Klacks.Api.Infrastructure.Scripting;

public enum ScriptValueType : byte
{
    Null = 0,
    Number = 1,
    Boolean = 2,
    String = 3,
    Object = 4
}

[StructLayout(LayoutKind.Auto)]
public readonly struct ScriptValue : IEquatable<ScriptValue>
{
    private readonly ScriptValueType _type;
    private readonly double _numberValue;
    private readonly object? _objectValue;

    private ScriptValue(ScriptValueType type, double numberValue, object? objectValue)
    {
        _type = type;
        _numberValue = numberValue;
        _objectValue = objectValue;
    }

    public static ScriptValue Null => new(ScriptValueType.Null, 0, null);

    public static ScriptValue FromNumber(double value) => new(ScriptValueType.Number, value, null);

    public static ScriptValue FromInt(int value) => new(ScriptValueType.Number, value, null);

    public static ScriptValue FromBoolean(bool value) => new(ScriptValueType.Boolean, value ? 1 : 0, null);

    public static ScriptValue FromString(string? value) =>
        value == null ? Null : new(ScriptValueType.String, 0, value);

    public static ScriptValue FromObject(object? value)
    {
        if (value == null) return Null;
        if (value is ScriptValue sv) return sv;
        if (value is double d) return FromNumber(d);
        if (value is int i) return FromInt(i);
        if (value is float f) return FromNumber(f);
        if (value is long l) return FromNumber(l);
        if (value is decimal dec) return FromNumber((double)dec);
        if (value is bool b) return FromBoolean(b);
        if (value is string s) return FromString(s);
        return new(ScriptValueType.Object, 0, value);
    }

    public ScriptValueType Type => _type;
    public bool IsNull => _type == ScriptValueType.Null;
    public bool IsNumber => _type == ScriptValueType.Number;
    public bool IsBoolean => _type == ScriptValueType.Boolean;
    public bool IsString => _type == ScriptValueType.String;
    public bool IsObject => _type == ScriptValueType.Object;

    public double AsDouble() => _type switch
    {
        ScriptValueType.Number => _numberValue,
        ScriptValueType.Boolean => _numberValue,
        ScriptValueType.String when double.TryParse((string?)_objectValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => d,
        ScriptValueType.Object when _objectValue != null => Convert.ToDouble(_objectValue, CultureInfo.InvariantCulture),
        _ => 0
    };

    public int AsInt() => (int)AsDouble();

    public bool AsBoolean() => _type switch
    {
        ScriptValueType.Boolean => _numberValue != 0,
        ScriptValueType.Number => _numberValue != 0,
        ScriptValueType.String => !string.IsNullOrEmpty((string?)_objectValue),
        ScriptValueType.Object => _objectValue != null,
        _ => false
    };

    public string AsString() => _type switch
    {
        ScriptValueType.String => (string?)_objectValue ?? string.Empty,
        ScriptValueType.Number => double.IsPositiveInfinity(_numberValue) ? "∞" :
                                  double.IsNegativeInfinity(_numberValue) ? "-∞" :
                                  _numberValue.ToString(CultureInfo.InvariantCulture),
        ScriptValueType.Boolean => _numberValue != 0 ? "True" : "False",
        ScriptValueType.Object => _objectValue?.ToString() ?? string.Empty,
        _ => string.Empty
    };

    public object? AsObject() => _type switch
    {
        ScriptValueType.Null => null,
        ScriptValueType.Number => _numberValue,
        ScriptValueType.Boolean => _numberValue != 0,
        ScriptValueType.String => _objectValue,
        ScriptValueType.Object => _objectValue,
        _ => null
    };

    public override string ToString() => AsString();

    public bool Equals(ScriptValue other)
    {
        if (_type == other._type)
        {
            return _type switch
            {
                ScriptValueType.Null => true,
                ScriptValueType.Number => _numberValue == other._numberValue,
                ScriptValueType.Boolean => _numberValue == other._numberValue,
                ScriptValueType.String => string.Equals((string?)_objectValue, (string?)other._objectValue, StringComparison.Ordinal),
                ScriptValueType.Object => Equals(_objectValue, other._objectValue),
                _ => false
            };
        }

        if ((_type == ScriptValueType.Boolean && other._type == ScriptValueType.Number) ||
            (_type == ScriptValueType.Number && other._type == ScriptValueType.Boolean))
        {
            return _numberValue == other._numberValue;
        }

        return false;
    }

    public override bool Equals(object? obj) => obj is ScriptValue other && Equals(other);

    public override int GetHashCode() => _type switch
    {
        ScriptValueType.Null => 0,
        ScriptValueType.Number => _numberValue.GetHashCode(),
        ScriptValueType.Boolean => _numberValue.GetHashCode(),
        _ => _objectValue?.GetHashCode() ?? 0
    };

    public static bool operator ==(ScriptValue left, ScriptValue right) => left.Equals(right);
    public static bool operator !=(ScriptValue left, ScriptValue right) => !left.Equals(right);

    public static ScriptValue operator +(ScriptValue left, ScriptValue right) =>
        FromNumber(left.AsDouble() + right.AsDouble());

    public static ScriptValue operator -(ScriptValue left, ScriptValue right) =>
        FromNumber(left.AsDouble() - right.AsDouble());

    public static ScriptValue operator *(ScriptValue left, ScriptValue right) =>
        FromNumber(left.AsDouble() * right.AsDouble());

    public static ScriptValue operator /(ScriptValue left, ScriptValue right) =>
        FromNumber(left.AsDouble() / right.AsDouble());

    public static ScriptValue operator %(ScriptValue left, ScriptValue right) =>
        FromNumber(left.AsDouble() % right.AsDouble());

    public static ScriptValue operator -(ScriptValue value) =>
        FromNumber(-value.AsDouble());

    public static ScriptValue operator !(ScriptValue value) =>
        FromBoolean(!value.AsBoolean());

    public static implicit operator ScriptValue(double value) => FromNumber(value);
    public static implicit operator ScriptValue(int value) => FromInt(value);
    public static implicit operator ScriptValue(bool value) => FromBoolean(value);
    public static implicit operator ScriptValue(string? value) => FromString(value);
}
