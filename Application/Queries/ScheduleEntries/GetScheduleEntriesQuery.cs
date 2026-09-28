// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Queries.ScheduleEntries;

public record GetScheduleEntriesQuery(WorkScheduleFilter Filter) : IRequest<WorkScheduleResponse>;
