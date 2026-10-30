// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.ComplianceEnforcement;

/// <summary>
/// The deterministic findings the compliance enforcement change advisor can raise.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ComplianceEnforcementFindingCode>))]
public enum ComplianceEnforcementFindingCode
{
    AlreadyBlocking = 0,
    NoWarningsInWindow = 1,
    WarningsBecomeErrors = 2,
    CountIsLowerBound = 3,
    CountedPerPeriod = 4,
    CountIsUpperBound = 5,
    SupervisorOverrideOn = 6,
    SupervisorOverrideOff = 7
}
