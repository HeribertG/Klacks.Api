// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Works;

public record ReopenPeriodCommand(DateOnly StartDate, DateOnly EndDate) : IRequest<int>;
