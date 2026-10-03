// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Soft-deletes a Proposed or Rejected planning constraint; Approved and Revoked rows stay for the audit trail.
/// </summary>
/// <param name="Id">Constraint to delete</param>
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.PlanningConstraints;

public record DeletePlanningConstraintCommand(Guid Id) : IRequest<PlanningConstraintResource>;
