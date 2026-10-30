// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.ComplianceEnforcement;

/// <summary>
/// The effect of switching one rule to Block, counted over the window; the counts are lower bounds when the evaluation says so.
/// </summary>
/// <param name="Rule">Rule name (ComplianceRuleNames)</param>
/// <param name="CurrentMode">The effective reaction of the rule now</param>
/// <param name="WarningsTurningIntoErrors">Warnings of the rule in the window that would become errors; 0 when the rule already blocks</param>
/// <param name="ErrorsAlreadyRaised">Errors of the rule in the window that exist because it already blocks</param>
/// <param name="Outcome">The computed outcome</param>
public sealed record ComplianceRuleEffect(
    string Rule,
    [property: JsonConverter(typeof(JsonStringEnumConverter<RuleEnforcementMode>))] RuleEnforcementMode CurrentMode,
    int WarningsTurningIntoErrors,
    int ErrorsAlreadyRaised,
    ComplianceRuleOutcome Outcome);
