// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.PeriodClosing;

public record ReopenPeriodByGroupCommand(
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? GroupId,
    string Reason
) : IRequest<int>;
