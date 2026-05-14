// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.PeriodClosing;

/// <summary>
/// Query for aggregating issues, warnings and notes within a billing period.
/// </summary>
/// <param name="From">Start of the date range (inclusive)</param>
/// <param name="To">End of the date range (inclusive)</param>
/// <param name="GroupId">Optional group scope; null returns issues across all clients</param>
public record GetPeriodIssuesQuery(
    DateOnly From,
    DateOnly To,
    Guid? GroupId
) : IRequest<List<PeriodIssueDto>>;
