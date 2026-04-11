// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.PeriodClosing;

/// <summary>
/// Query for retrieving per-day work and break sealing counts within a date range.
/// </summary>
/// <param name="From">Start of the date range (inclusive)</param>
/// <param name="To">End of the date range (inclusive)</param>
/// <param name="GroupId">Optional group scope; null returns counts across all groups</param>
public record GetSealedPeriodsQuery(
    DateOnly From,
    DateOnly To,
    Guid? GroupId
) : IRequest<List<SealedPeriodSummaryDto>>;
