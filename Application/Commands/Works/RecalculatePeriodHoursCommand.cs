// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Works;

public record RecalculatePeriodHoursCommand(DateOnly StartDate, DateOnly EndDate, Guid? SelectedGroup = null) : IRequest<bool>;
