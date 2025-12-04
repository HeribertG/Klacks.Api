using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.ValueObjects;

public sealed partial class PhoneNumber : IEquatable<PhoneNumber>
{
    private static readonly Regex PhoneRegex = GeneratePhoneRegex();

    public string Value { get; }
    public string Normalized { get; }

    private PhoneNumber(string value, string normalized)
    {
        Value = value;
        Normalized = normalized;
    }

    public static PhoneNumber Create(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return new PhoneNumber(string.Empty, string.Empty);

        var trimmed = phoneNumber.Trim();
        var normalized = Normalize(trimmed);

        return new PhoneNumber(trimmed, normalized);
    }

    public static PhoneNumber CreateOrEmpty(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return new PhoneNumber(string.Empty, string.Empty);

        return Create(phoneNumber);
    }

    public static bool IsValid(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var normalized = Normalize(phoneNumber);
        return normalized.Length >= 7 && normalized.Length <= 15;
    }

    private static string Normalize(string phoneNumber)
    {
        return PhoneRegex.Replace(phoneNumber, string.Empty);
    }

    public bool IsEmpty => string.IsNullOrEmpty(Value);

    public override string ToString() => Value;

    public override bool Equals(object? obj) => obj is PhoneNumber other && Equals(other);

    public bool Equals(PhoneNumber? other) =>
        other is not null && Normalized == other.Normalized;

    public override int GetHashCode() => Normalized.GetHashCode();

    public static bool operator ==(PhoneNumber? left, PhoneNumber? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(PhoneNumber? left, PhoneNumber? right) => !(left == right);

    public static implicit operator string(PhoneNumber phone) => phone.Value;

    [GeneratedRegex(@"[\s\-\(\)\.]+", RegexOptions.Compiled)]
    private static partial Regex GeneratePhoneRegex();
}
