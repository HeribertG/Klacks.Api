// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads one planning constraint.
/// </summary>
/// <param name="Id">Constraint id</param>
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.PlanningConstraints;

public record GetPlanningConstraintQuery(Guid Id) : IRequest<PlanningConstraintResource?>;
