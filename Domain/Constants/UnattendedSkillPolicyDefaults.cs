// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Autonomy thresholds the unattended (background) skill policy enforces per risk class. They are
/// deliberately STRICTER than the interactive chat matrix in AutonomyGateService.IsAllowed, because on a
/// background path nobody can be asked to confirm anything. ScenarioGated sits lower than Reversible on
/// purpose: a scenario-gated skill only writes into an AnalyseScenario a human still has to accept,
/// while a reversible skill already changes live data and merely has an inverse skill.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class UnattendedSkillPolicyDefaults
{
    public const AutonomyLevel MinimumLevelForReversible = AutonomyLevel.Autonomous;

    public const AutonomyLevel MinimumLevelForScenarioGated = AutonomyLevel.Assisted;

    public const AutonomyLevel MinimumLevelForIrreversibleOptIn = AutonomyLevel.Autonomous;

    /// <summary>
    /// Threshold for an irreversible skill on the email-automation path, which has no per-task opt-in.
    /// Set to the highest level because every irreversible action the email intent mapping can trigger
    /// already demands FullyAutonomous there — the policy re-states that requirement instead of
    /// trusting the mapping to keep it.
    /// </summary>
    public const AutonomyLevel MinimumLevelForIrreversibleEmailAutomation = AutonomyLevel.FullyAutonomous;
}
