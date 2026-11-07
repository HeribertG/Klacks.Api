// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Works;

/// <summary>
/// Queues a thorough recalculation of all schedule entries in the given range.
/// The actual work is processed asynchronously by ThoroughRecalculationBackgroundService:
/// re-runs Work, WorkChange and Break macros, then recalculates all period hours. Frozen periods keep their cached
/// period hours unless ConfirmFrozen is set with a reason (admin override, audited).
/// </summary>
/// <param name="StartDate">Inclusive period start</param>
/// <param name="EndDate">Inclusive period end</param>
/// <param name="SelectedGroup">Optional group scope; when null all clients are processed</param>
/// <param name="AnalyseToken">Optional scenario token; when null operates on real data</param>
/// <param name="ConfirmFrozen">Admin override: recalculate the period hours of frozen periods too</param>
/// <param name="Reason">Mandatory with ConfirmFrozen; stored in the period audit log</param>
public record RecalculatePeriodHoursThoroughCommand(
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? SelectedGroup = null,
    Guid? AnalyseToken = null,
    bool ConfirmFrozen = false,
    string? Reason = null) : IRequest<PeriodHoursRecalculationResult>;
