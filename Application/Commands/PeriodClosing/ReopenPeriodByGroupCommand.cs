// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.PeriodClosing;

public record ReopenPeriodByGroupCommand(
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? GroupId,
    string Reason
) : IRequest<PeriodReopenResult>;
