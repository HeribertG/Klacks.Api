// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Revokes an Approved planning constraint; the row stays for the audit trail but is no longer evaluated.
/// </summary>
/// <param name="Id">Constraint to revoke</param>
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.PlanningConstraints;

public record RevokePlanningConstraintCommand(Guid Id) : IRequest<PlanningConstraintResource>;
