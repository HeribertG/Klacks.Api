// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.ComplianceEnforcement;

/// <summary>
/// What switching one rule to Block does to the findings of the window.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ComplianceRuleOutcome>))]
public enum ComplianceRuleOutcome
{
    AlreadyBlocking = 0,
    NoWarningsInWindow = 1,
    WarningsBecomeErrors = 2
}
