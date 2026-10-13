// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Checks that a value typed into a registration form is exactly one plain e-mail address: no display name,
/// no angle brackets, no second address and nothing around it except surrounding whitespace.
/// </summary>
/// <param name="value">The raw input; null and blank are not well-formed</param>
/// <param name="normalized">The trimmed address when the input is well-formed, otherwise an empty string</param>

using System.Net.Mail;

namespace Klacks.Api.Domain.Helpers;

public static class EmailAddressFormat
{
    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (!MailAddress.TryCreate(trimmed, out var address)
            || !string.Equals(address.Address, trimmed, StringComparison.Ordinal))
        {
            return false;
        }

        normalized = trimmed;
        return true;
    }
}
