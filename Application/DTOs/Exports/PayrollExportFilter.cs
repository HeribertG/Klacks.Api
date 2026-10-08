// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Filter for a manual, on-demand payroll export of a closed period.
/// @param FromDate - Lower bound (inclusive) for closed work entries
/// @param UntilDate - Upper bound (inclusive) for closed work entries
/// @param Language - Culture name carried into the export metadata
/// @param Format - Payroll FormatKey (e.g. datev-lug-bewegungsdaten, paxml-se) selecting the formatter
/// @param ClientIds - Optional person selection for a supplementary export; null exports every new or changed person
/// </summary>
namespace Klacks.Api.Application.DTOs.Exports;

public class PayrollExportFilter
{
    public DateOnly FromDate { get; set; }

    public DateOnly UntilDate { get; set; }

    public string Language { get; set; } = "de";

    public string Format { get; set; } = string.Empty;

    public List<Guid>? ClientIds { get; set; }
}
