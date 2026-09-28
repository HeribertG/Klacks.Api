// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Works;

public record ReopenPeriodCommand(DateOnly StartDate, DateOnly EndDate) : IRequest<int>;
