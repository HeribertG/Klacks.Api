// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Changes a planning constraint. A Proposed row is edited in place; an Approved row is immutable, so the change becomes a new Approved row linked via PreviousVersionId while the old row is Revoked. Rejected and Revoked rows cannot be changed.
/// </summary>
/// <param name="Id">Constraint to change</param>
/// <param name="Resource">New editable fields</param>
/// <param name="Actor">User id of the admin making the change</param>
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.PlanningConstraints;

public record UpdatePlanningConstraintCommand(Guid Id, PlanningConstraintWriteResource Resource, string Actor) : IRequest<PlanningConstraintResource>;
