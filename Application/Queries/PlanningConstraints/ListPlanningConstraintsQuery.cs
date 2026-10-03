// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists planning constraints, newest first; with a status only that status (Proposed = the UI pending list).
/// </summary>
/// <param name="Status">Optional approval-status filter</param>
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.PlanningConstraints;

public record ListPlanningConstraintsQuery(RuleApprovalStatus? Status) : IRequest<IReadOnlyList<PlanningConstraintResource>>;
