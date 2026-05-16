// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// On-demand validator that mirrors the SignalR background validator for a closed period.
/// Returns rest/overtime/consecutive-day/collision findings as PeriodIssueDto entries so
/// the period-closing UI can show them next to ScheduleNotes without waiting for a SignalR push.
/// </summary>
using Klacks.Api.Application.DTOs.PeriodClosing;

namespace Klacks.Api.Application.Interfaces.PeriodClosing;

public interface IPeriodValidationLoader
{
    Task<List<PeriodIssueDto>> LoadAsync(
        DateOnly from,
        DateOnly to,
        Guid? groupId,
        CancellationToken cancellationToken = default);
}
