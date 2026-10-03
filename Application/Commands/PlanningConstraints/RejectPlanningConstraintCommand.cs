// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Rejects a Proposed planning constraint from the UI pending list.
/// </summary>
/// <param name="Id">Constraint to reject</param>
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.PlanningConstraints;

public record RejectPlanningConstraintCommand(Guid Id) : IRequest<PlanningConstraintResource>;
