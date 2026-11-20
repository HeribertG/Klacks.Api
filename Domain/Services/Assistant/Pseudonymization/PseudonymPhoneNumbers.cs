// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Digit helpers for phone numbers: two spellings of the same number ("+41 44 123 45 67", "044 123 45 67",
/// "441234567") share their last nine digits, which is how a number stored in the communication table is recognised
/// in text whatever prefix it carries there.
/// </summary>

using System.Text;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

public static class PseudonymPhoneNumbers
{
    public static string Digits(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (c is >= '0' and <= '9')
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    public static string ComparisonTail(string value)
    {
        var digits = Digits(value);
        return digits.Length > PseudonymizationConstants.PhoneComparisonDigits
            ? digits[^PseudonymizationConstants.PhoneComparisonDigits..]
            : digits;
    }
}
