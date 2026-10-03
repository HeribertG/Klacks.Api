// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Approves a Proposed, not expired planning constraint from the UI pending list (the only approval path - there is deliberately no skill for it).
/// </summary>
/// <param name="Id">Constraint to approve</param>
/// <param name="Actor">User id of the approving admin</param>
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.PlanningConstraints;

public record ApprovePlanningConstraintCommand(Guid Id, string Actor) : IRequest<PlanningConstraintResource>;
