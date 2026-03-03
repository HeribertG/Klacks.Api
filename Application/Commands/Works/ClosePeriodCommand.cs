// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Works;

public record ClosePeriodCommand(DateOnly StartDate, DateOnly EndDate) : IRequest<int>;
