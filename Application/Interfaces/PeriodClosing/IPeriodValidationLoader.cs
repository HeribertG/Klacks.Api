// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// On-demand validator that mirrors the SignalR background validator for a closed period.
/// Returns rest/overtime/consecutive-day/weekly-overtime/min-rest-days/collision findings as
/// PeriodIssueDto entries so the period-closing UI and the detect_conflicts skill can surface
/// them without waiting for a SignalR push. When analyseToken is set, the isolated scenario is
/// validated instead of the real plan.
/// </summary>
using Klacks.Api.Application.DTOs.PeriodClosing;

namespace Klacks.Api.Application.Interfaces.PeriodClosing;

public interface IPeriodValidationLoader
{
    Task<List<PeriodIssueDto>> LoadAsync(
        DateOnly from,
        DateOnly to,
        Guid? groupId,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default);
}
