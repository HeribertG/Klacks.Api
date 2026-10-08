// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Computes the content hash of one person's payroll day entries: SHA-256 (lowercase hex) over the entries sorted by
/// date, kind, absence, unit and quantity, one line per entry in the form date|kind|quantity|unit|absenceId, behind
/// the payload version prefix HashVersionPrefix. The prefix makes a deliberate change of the hashed content (for
/// example a new kind of payroll entry) possible: bumping it changes every hash once, which is an explicit decision
/// and never an accident. The hash is culture-invariant, independent of entry order and of the export format; two
/// exports of a person are treated as identical exactly when their hashes are equal.
/// </summary>
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Domain.Services.Exports;

public static class PayrollPersonContentHash
{
    public const int QuantityDecimals = 4;

    public const string DateFormat = "yyyy-MM-dd";

    public const string HashVersionPrefix = "v1|";

    private const decimal TrailingZeroNormalizer = 1.0000000000000000000000000000m;
    private static readonly string QuantityFormat = "0." + new string('#', QuantityDecimals);
    private const char FieldSeparator = '|';
    private const char LineSeparator = '\n';

    public static string Compute(IEnumerable<PayrollDayEntry> entries)
    {
        var lines = entries
            .OrderBy(e => e.Date)
            .ThenBy(e => (int)e.Kind)
            .ThenBy(e => e.AbsenceId)
            .ThenBy(e => (int)e.Unit)
            .ThenBy(e => NormalizeQuantity(e.Quantity))
            .Select(FormatLine);

        var payload = HashVersionPrefix + string.Join(LineSeparator, lines);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(hash);
    }

    public static decimal NormalizeQuantity(decimal quantity)
    {
        var rounded = Math.Round(quantity, QuantityDecimals, MidpointRounding.AwayFromZero);
        return rounded == 0m ? 0m : rounded / TrailingZeroNormalizer;
    }

    private static string FormatLine(PayrollDayEntry entry)
    {
        return string.Concat(
            entry.Date.ToString(DateFormat, CultureInfo.InvariantCulture),
            FieldSeparator,
            ((int)entry.Kind).ToString(CultureInfo.InvariantCulture),
            FieldSeparator,
            NormalizeQuantity(entry.Quantity).ToString(QuantityFormat, CultureInfo.InvariantCulture),
            FieldSeparator,
            ((int)entry.Unit).ToString(CultureInfo.InvariantCulture),
            FieldSeparator,
            entry.AbsenceId?.ToString() ?? string.Empty);
    }
}
