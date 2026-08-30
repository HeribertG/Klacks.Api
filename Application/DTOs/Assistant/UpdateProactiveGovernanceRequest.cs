// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public class UpdateProactiveGovernanceRequest
{
    public string? TriggerKind { get; set; }

    public Guid? GroupId { get; set; }

    public int? MaxAction { get; set; }

    public bool? Enabled { get; set; }

    public Guid? ResponsibleOwnerUserId { get; set; }

    /// <summary>
    /// Distinguishes "leave the responsible owner untouched" (false, the default) from "remove the
    /// owner" - which a null ResponsibleOwnerUserId alone cannot express in a patch request.
    /// </summary>
    public bool ClearResponsibleOwner { get; set; }

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
