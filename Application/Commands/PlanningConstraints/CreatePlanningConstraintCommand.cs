// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates a planning constraint as an admin: stored immediately as Origin Admin and Approved.
/// </summary>
/// <param name="Resource">Editable fields of the new constraint (no id - the server assigns it)</param>
/// <param name="Actor">User id of the deciding admin, recorded as proposer and approver</param>
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.PlanningConstraints;

public record CreatePlanningConstraintCommand(PlanningConstraintWriteResource Resource, string Actor) : IRequest<PlanningConstraintResource>;
