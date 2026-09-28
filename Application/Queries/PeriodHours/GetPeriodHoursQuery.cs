// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;

namespace Klacks.Api.Application.Queries.PeriodHours;

public record GetPeriodHoursQuery(PeriodHoursRequest Request) : IRequest<Dictionary<Guid, PeriodHoursResource>>;
