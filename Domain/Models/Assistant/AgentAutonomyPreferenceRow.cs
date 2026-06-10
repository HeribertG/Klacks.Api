// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistent per-user autonomy level for Klacksy. Controls how much the assistant may
/// execute without an explicit user confirmation (see AutonomyLevel / SkillRiskClass).
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public class AgentAutonomyPreferenceRow : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    public AutonomyLevel Level { get; set; } = AutonomyDefaults.DefaultLevel;
}
