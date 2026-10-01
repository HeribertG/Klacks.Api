// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Splits a free-text phone number into the country prefix and the national number the way the client
/// communication record stores them (Prefix + Value). A leading "00" becomes "+"; a number that already
/// starts with the country's prefix is split off, a national number with a trunk zero gets the prefix;
/// a foreign international number stays whole in the value. Shared by the create_employee skill and the
/// employee import.
/// </summary>

namespace Klacks.Api.Application.Common;

public static class PhoneNumberSplitter
{
    private const string InternationalPlus = "+";
    private const string InternationalDoubleZero = "00";
    private const char TrunkZero = '0';
    private const char PlusSign = '+';

    /// <param name="phone">The phone number as typed, with any separators</param>
    /// <param name="countryPrefix">The country calling code of the owner's country, e.g. "+41"; empty when unknown</param>
    public static (string Prefix, string Number) Split(string? phone, string? countryPrefix)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return (string.Empty, string.Empty);
        }

        var cleaned = new string(phone.Where(ch => char.IsDigit(ch) || ch == PlusSign).ToArray());
        if (cleaned.StartsWith(InternationalDoubleZero, StringComparison.Ordinal))
        {
            cleaned = InternationalPlus + cleaned[InternationalDoubleZero.Length..];
        }

        var prefix = countryPrefix ?? string.Empty;

        if (!string.IsNullOrEmpty(prefix) && cleaned.StartsWith(prefix, StringComparison.Ordinal))
        {
            return (prefix, cleaned[prefix.Length..].TrimStart(TrunkZero));
        }

        if (cleaned.StartsWith(PlusSign))
        {
            return (string.Empty, cleaned);
        }

        if (!string.IsNullOrEmpty(prefix))
        {
            return (prefix, cleaned.TrimStart(TrunkZero));
        }

        return (string.Empty, cleaned);
    }
}
