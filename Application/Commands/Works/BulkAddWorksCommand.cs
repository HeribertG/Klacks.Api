// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Commands.Works;

/// <summary>
/// Creates several Works in one call. Every caller is held to the employee planning periods (membership and group
/// period, like the grid's locked cells) unless it is a planner that already applies its own period rules to its output.
/// </summary>
/// <param name="Request">The Works to create; bound from the REST body, so it can never switch the period check off</param>
/// <param name="PeriodCheck">Enforce for REST and chat (the whole request is refused). SkipForPlannerOutput only for
/// callers that filter their placements one by one before they write, so one bad cell does not void a whole run: the
/// wizard apply and propose_plan (the planner models the membership per day but not the group period, so the apply
/// filters both against the scenario's group), and the harmonizer's recreation of uncorrelated Works - which may put a
/// Work on another employee or day and is NOT filtered (open gap, see IClientPlanningPeriodGuard)</param>
public record BulkAddWorksCommand(
    BulkAddWorksRequest Request,
    PlanningPeriodCheck PeriodCheck = PlanningPeriodCheck.Enforce) : IRequest<BulkWorksResponse>;
