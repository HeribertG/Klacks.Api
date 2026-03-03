// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.ValueObjects;

public sealed partial class Email : IEquatable<Email>
{
    private static readonly Regex EmailRegex = GenerateEmailRegex();

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return new Email(string.Empty);

        var trimmed = email.Trim().ToLowerInvariant();

        if (!IsValid(trimmed))
            throw new ArgumentException($"Invalid email format: {email}", nameof(email));

        return new Email(trimmed);
    }

    public static Email CreateOrEmpty(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return new Email(string.Empty);

        var trimmed = email.Trim().ToLowerInvariant();

        return IsValid(trimmed) ? new Email(trimmed) : new Email(string.Empty);
    }

    public static bool IsValid(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return EmailRegex.IsMatch(email);
    }

    public bool IsEmpty => string.IsNullOrEmpty(Value);

    public override string ToString() => Value;

    public override bool Equals(object? obj) => obj is Email other && Equals(other);

    public bool Equals(Email? other) => other is not null && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(Email? left, Email? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(Email? left, Email? right) => !(left == right);

    public static implicit operator string(Email email) => email.Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex GenerateEmailRegex();
}
