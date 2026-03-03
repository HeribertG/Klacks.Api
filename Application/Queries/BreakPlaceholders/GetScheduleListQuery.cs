// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.BreakPlaceholders;

public record GetScheduleListQuery(BreakFilter Filter)
    : IRequest<IEnumerable<ClientBreakPlaceholderResource>>;
