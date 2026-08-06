// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Scheduling;

/// <summary>
/// One contract that still references a scheduling rule whose industry is no longer active. The
/// assignment is deliberately left in place - a contract is a legal document and is never rewritten
/// automatically - so this is what the admin needs in order to migrate it deliberately.
/// </summary>
/// <param name="ContractId">The contract still pointing at the rule</param>
/// <param name="ContractName">Name of that contract, for the admin to recognise it</param>
/// <param name="SchedulingRuleId">The currently assigned rule</param>
/// <param name="SchedulingRuleName">Name of that rule</param>
/// <param name="Industry">Industry slug of the rule, which is no longer among the active ones</param>
/// <param name="AffectedClientCount">How many clients are attached to the contract</param>
public sealed record IndustryMigrationCandidate(
    Guid ContractId,
    string ContractName,
    Guid SchedulingRuleId,
    string SchedulingRuleName,
    string Industry,
    int AffectedClientCount);
