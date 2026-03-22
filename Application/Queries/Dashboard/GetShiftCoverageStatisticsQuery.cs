// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query for retrieving shift coverage and sealing statistics per group for the current month.
/// </summary>
using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Dashboard;

public record GetShiftCoverageStatisticsQuery : IRequest<IEnumerable<ShiftCoverageStatisticsResource>>;
