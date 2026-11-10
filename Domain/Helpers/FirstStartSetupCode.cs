// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Compares the one-time setup code a visitor typed with the code the installer stored. Both are normalised first
/// (dashes and whitespace removed, upper case), so "abcd-efgh" matches "ABCDEFGH". The comparison runs on SHA-256
/// digests of equal length with CryptographicOperations.FixedTimeEquals, so neither the content nor the length of the
/// stored code can be probed through response times. A missing stored code, a missing input or an input longer than
/// MaxSuppliedLength never matches; the length cap bounds the work an anonymous caller can cause.
/// </summary>

using System.Security.Cryptography;
using System.Text;

namespace Klacks.Api.Domain.Helpers;

public static class FirstStartSetupCode
{
    public const int MaxSuppliedLength = 64;

    private const char GroupSeparator = '-';

    public static bool IsConfigured(string? storedCode) => Normalize(storedCode).Length > 0;

    public static bool Matches(string? storedCode, string? suppliedCode)
    {
        if (suppliedCode is not null && suppliedCode.Length > MaxSuppliedLength)
        {
            return false;
        }

        var stored = Normalize(storedCode);
        var supplied = Normalize(suppliedCode);
        if (stored.Length == 0 || supplied.Length == 0)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(Digest(stored), Digest(supplied));
    }

    private static string Normalize(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(code.Length);
        foreach (var character in code)
        {
            if (character != GroupSeparator && !char.IsWhiteSpace(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString();
    }

    private static byte[] Digest(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
