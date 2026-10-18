// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Result of a booking diagnosis for one cell. NotFound carries no name, no finding and no advisory: a hidden employee,
/// shift or scenario reads exactly like a missing one.
/// </summary>
/// <param name="Outcome">Bookable, Refused (a write path refuses), GridOnly (only the grid blocks) or NotFound</param>
/// <param name="ClientName">Display name of the employee; null for NotFound</param>
/// <param name="ShiftName">Name of the shift; null for NotFound</param>
/// <param name="Refusals">Every refusal that applies now, in the order the direct entry path checks them</param>
/// <param name="Advisories">Findings the booking would introduce without being refused</param>
public sealed record WorkBookingDiagnosis(
    WorkBookingDiagnosisOutcome Outcome,
    string? ClientName,
    string? ShiftName,
    IReadOnlyList<WorkBookingRefusalFinding> Refusals,
    IReadOnlyList<WorkBookingAdvisory> Advisories)
{
    public static WorkBookingDiagnosis NotFound { get; } = new(WorkBookingDiagnosisOutcome.NotFound, null, null, [], []);

    /// <summary>The code the direct entry path answers with, i.e. its first refusal; null when it would save.</summary>
    public string? DirectEntryCode => Refusals.FirstOrDefault(refusal => refusal.Channels.DirectEntry)?.Code;
}
