// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Strict parser for a payment interval given as text. It matches the member names case-insensitively and
/// nothing else: Enum.TryParse would also accept a comma list ("Weekly,Monthly" becomes the bit-or Monthly)
/// and raw numbers, which would silently turn a malformed value into a different interval.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Contracts;

public static class ContractPaymentIntervalParser
{
    public static bool TryParse(string? raw, out PaymentInterval interval, out string? error)
    {
        var trimmed = (raw ?? string.Empty).Trim();
        var names = Enum.GetNames<PaymentInterval>();
        var match = names.FirstOrDefault(name => string.Equals(name, trimmed, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            interval = default;
            error = $"Invalid paymentInterval '{trimmed}'. Use one of: {string.Join(", ", names)}.";
            return false;
        }

        interval = Enum.Parse<PaymentInterval>(match);
        error = null;
        return true;
    }
}
