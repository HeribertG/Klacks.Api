// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public class UpdateProactiveGovernanceRequest
{
    public string? TriggerKind { get; set; }

    public Guid? GroupId { get; set; }

    public int? MaxAction { get; set; }

    public bool? Enabled { get; set; }

    public int? DailyActionBudget { get; set; }

    public int? WindowActionLimit { get; set; }

    public int? WindowMinutes { get; set; }

    public bool? KillSwitch { get; set; }

    /// <summary>
    /// The installation-wide autonomy level (0-3, AutonomyLevel) that caps every rule's MaxAction.
    /// Null leaves the stored level untouched.
    /// </summary>
    public int? AutonomyLevel { get; set; }
}
