// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Works;

public record RecalculatePeriodHoursCommand(DateOnly StartDate, DateOnly EndDate, Guid? SelectedGroup = null) : IRequest<bool>;
