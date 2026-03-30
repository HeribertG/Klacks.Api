// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Export entry for a work change (correction or replacement).
/// @param Type - The type of change (CorrectionStart/End, ReplacementStart/End)
/// @param ChangeTime - Hours affected by the change
/// @param ToInvoice - Whether this change should be invoiced
/// </summary>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Exports;

public class WorkChangeExportEntry
{
    public WorkChangeType Type { get; set; }

    public decimal ChangeTime { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? ReplaceEmployeeName { get; set; }

    public decimal Surcharges { get; set; }

    public bool ToInvoice { get; set; }
}
