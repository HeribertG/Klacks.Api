// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Works;

/// <summary>
/// Queues a thorough recalculation of all schedule entries in the given range.
/// The actual work is processed asynchronously by ThoroughRecalculationBackgroundService:
/// re-runs Work, WorkChange and Break macros, then recalculates all period hours.
/// </summary>
/// <param name="StartDate">Inclusive period start</param>
/// <param name="EndDate">Inclusive period end</param>
/// <param name="SelectedGroup">Optional group scope; when null all clients are processed</param>
/// <param name="AnalyseToken">Optional scenario token; when null operates on real data</param>
public record RecalculatePeriodHoursThoroughCommand(
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? SelectedGroup = null,
    Guid? AnalyseToken = null) : IRequest<bool>;
