// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Bots;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Bots;

public record GetUnstaffedShiftSummaryQuery(string ClientName, DateOnly StartDate, DateOnly EndDate)
    : IRequest<UnstaffedShiftSummaryDto>;
