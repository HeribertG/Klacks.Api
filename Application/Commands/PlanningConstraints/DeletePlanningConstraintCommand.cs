// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Soft-deletes a planning constraint that is not Approved (an Approved row must be revoked first).
/// </summary>
/// <param name="Id">Constraint to delete</param>
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.PlanningConstraints;

public record DeletePlanningConstraintCommand(Guid Id) : IRequest<PlanningConstraintResource>;
