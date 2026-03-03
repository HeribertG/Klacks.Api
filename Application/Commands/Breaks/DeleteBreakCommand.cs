// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Commands.Breaks;

public record DeleteBreakCommand(
    Guid Id,
    DateOnly PeriodStart,
    DateOnly PeriodEnd) : IRequest<BreakResource?>;
