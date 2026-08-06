// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Scheduling;

/// <summary>
/// One deliberate move of a contract onto another scheduling rule.
/// </summary>
/// <param name="ContractId">The contract to move</param>
/// <param name="SchedulingRuleId">The rule it should reference from now on, null to clear it</param>
public sealed record ContractSchedulingRuleAssignment(Guid ContractId, Guid? SchedulingRuleId);
