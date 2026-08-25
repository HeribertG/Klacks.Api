// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One governance rule for a proactive trigger kind: how far Klacksy may act on that kind by itself,
/// under whose identity it acts, and how often. A row with a null GroupId is the installation-wide
/// rule for its kind; a row carrying a GroupId is the scope exception that overrides it for that group
/// alone (the column exists from the start so Etappe 6 needs no second migration). Only a human writes
/// these rows - the setting skill is classified Sensitive precisely so the heartbeat can never grant
/// itself more room.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public class AgentTriggerGovernance : BaseEntity
{
    /// <summary>The canonical trigger kind string from AgentTriggerKinds.</summary>
    public string TriggerKind { get; set; } = string.Empty;

    /// <summary>Null means the installation-wide rule; a value narrows the rule to that one group.</summary>
    public Guid? GroupId { get; set; }

    public ProactiveMaxAction MaxAction { get; set; } = ProactiveGovernanceDefaults.MaxAction;

    /// <summary>
    /// Whether Klacksy may handle this kind autonomously at all. False pins the kind to Hint; it never
    /// suppresses the notification itself, which stays governed by the per-user mute and snooze gates.
    /// </summary>
    public bool Enabled { get; set; } = ProactiveGovernanceDefaults.Enabled;

    /// <summary>
    /// The human under whose current roles a prepared or executed action runs (Etappe 4d issues an
    /// internal token for this user). Required from MaxAction Prepare upwards, enforced in the command
    /// handler rather than by a database constraint. Deliberately no foreign key: AppUser is the one
    /// entity without soft-delete, so a constraint would hard-fail on user deletion.
    /// </summary>
    public Guid? ResponsibleOwnerUserId { get; set; }

    public int DailyActionBudget { get; set; } = ProactiveGovernanceDefaults.DailyActionBudget;

    public int WindowActionLimit { get; set; } = ProactiveGovernanceDefaults.WindowActionLimit;

    public int WindowMinutes { get; set; } = ProactiveGovernanceDefaults.WindowMinutes;
}
