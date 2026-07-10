// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Filter for a manual, on-demand payroll export of a single group's closed period.
/// @param GroupId - The group (location/branch) whose closed period is exported; its
///   PayrollExportGroupConfig supplies delimiter, encoding and the wage-type/absence mapping
/// @param FromDate - Lower bound (inclusive) for closed work entries
/// @param UntilDate - Upper bound (inclusive) for closed work entries
/// @param Language - Culture name carried into the export metadata
/// @param Format - Payroll FormatKey (e.g. datev-lug-bewegungsdaten, paxml-se) selecting the formatter
/// </summary>
namespace Klacks.Api.Application.DTOs.Exports;

public class PayrollExportFilter
{
    public Guid GroupId { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly UntilDate { get; set; }

    public string Language { get; set; } = "de";

    public string Format { get; set; } = string.Empty;
}
