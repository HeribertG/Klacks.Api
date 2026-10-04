// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Culture-invariant formatting of the value fields that go into an ImportContentHash. The desired hash from the
/// profile file and the recomputed live-value hash of a stored row must format every field identically, so all
/// region-setup hashers share these formats; null becomes an empty field.
/// </summary>
/// <param name="value">Field value to format; null yields an empty string</param>

using System.Globalization;

namespace Klacks.Api.Application.Services.Setup;

public static class ImportFieldFormatter
{
    private const string DecimalFormat = "F4";
    private const string TrueValue = "true";
    private const string FalseValue = "false";

    public static string Decimal(decimal? value) =>
        value?.ToString(DecimalFormat, CultureInfo.InvariantCulture) ?? string.Empty;

    public static string Int(int? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public static string Bool(bool? value) =>
        value.HasValue ? (value.Value ? TrueValue : FalseValue) : string.Empty;
}
