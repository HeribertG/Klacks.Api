// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One reason why a payroll export is not allowed yet: a person, optionally a day, and the group whose close
/// would resolve it.
/// @param ClientId - The blocked person
/// @param ClientName - Display name of the person (Last, First)
/// @param IdNumber - Personnel number of the person
/// @param Date - The blocked day; null for blockers that concern the whole period (overlapping export)
/// @param GroupId - Group whose close resolves the blocker; null when none applies
/// @param GroupName - Name of GroupId, null when GroupId is null
/// @param Reason - Why the person or day blocks the export
/// @param RequiresGlobalClose - True when only a global period close can lock the day (no group applies)
/// @param EntryCount - Entries of the person on that day that are concerned (0 for overlapping exports)
/// </summary>
using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Exports;

public class PayrollExportBlockerDto
{
    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public int IdNumber { get; set; }

    public DateOnly? Date { get; set; }

    public Guid? GroupId { get; set; }

    public string? GroupName { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PayrollExportBlockReason Reason { get; set; }

    public bool RequiresGlobalClose { get; set; }

    public int EntryCount { get; set; }
}
