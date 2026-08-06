// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.SchedulingRules;

/// <summary>
/// Moves the listed contracts to the scheduling rules the admin picked for them. This is the
/// deliberate counterpart to the migration list: an ACTIVE_INDUSTRIES switch never rewrites a
/// contract by itself, an admin does, one decision per contract.
/// </summary>
/// <param name="Assignments">Which contract moves to which rule</param>
public sealed record MigrateContractSchedulingRulesCommand(
    IReadOnlyList<ContractSchedulingRuleAssignment> Assignments)
    : IRequest<int>;
