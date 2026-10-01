// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Recognises what a cell value looks like (e-mail, phone number, date, postcode, "postcode city") for
/// the content-based column detection and the e-mail validation of the import.
/// </summary>

using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Klacks.Api.Application.Services.ClientImport;

public static class ClientImportValueClassifier
{
    private const int MinPhoneDigits = 7;
    private const int MaxPhoneDigits = 15;
    private const char At = '@';

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

    private static readonly Regex PhonePattern = new(
        @"^\s*\+?[\d\s()./\-]+$", RegexOptions.CultureInvariant, RegexTimeout);

    private static readonly Regex ZipPattern = new(
        @"^\s*\d{4,5}\s*$", RegexOptions.CultureInvariant, RegexTimeout);

    public static bool IsEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains(' ') || value.IndexOf(At) <= 0)
        {
            return false;
        }

        return MailAddress.TryCreate(value.Trim(), out var address)
            && string.Equals(address.Address, value.Trim(), StringComparison.OrdinalIgnoreCase)
            && address.Host.Contains('.');
    }

    public static bool IsPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !PhonePattern.IsMatch(value))
        {
            return false;
        }

        var digits = value.Count(char.IsDigit);
        return digits is >= MinPhoneDigits and <= MaxPhoneDigits;
    }

    public static bool IsDate(string? value) => ClientImportDateParser.TrySplit(value, out _, out _, out _);

    public static bool IsZip(string? value) => !string.IsNullOrWhiteSpace(value) && ZipPattern.IsMatch(value);

    public static bool IsZipCity(string? value) => ClientImportAddressSplitter.TrySplitZipCity(value, out _, out _);
}
