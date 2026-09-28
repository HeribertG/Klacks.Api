// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Commands.Breaks;

public record DeleteBreakCommand(
    Guid Id,
    DateOnly PeriodStart,
    DateOnly PeriodEnd) : IRequest<BreakResource?>;
