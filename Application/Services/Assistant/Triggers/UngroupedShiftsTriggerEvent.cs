// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when an installation that already works with groups carries at least
/// UngroupedShiftsDetector.MinUngroupedShiftsForRecommendation staffable duties that belong to no
/// group. Severity Low keeps it in the inbox and the badge and out of the live push: nothing is
/// broken right now, but every one of those duties stays outside the group-scoped period close and
/// outside payroll by group.
///
/// The sentence must not claim that a missing membership hides the duty from anybody. Whether a
/// planner sees an ungrouped duty depends on the show_ungrouped_shifts setting, not on the membership
/// alone, so such a claim would be wrong in every installation that has the setting switched on.
///
/// The DedupKey is a CONSTANT and carries neither the count nor a day. Dispatch dedup has no time
/// window (ProactiveTriggerDispatchRepository.WasDispatchedAsync), so a per-day key would re-open the
/// finding every midnight while the row it replaced never resolved, and a key carrying the count would
/// do the same on every tick that groups or ungroups a single duty.
/// </summary>
/// <param name="UngroupedShiftCount">Staffable duties without any group the recommendation is sized against.</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record UngroupedShiftsTriggerEvent(int UngroupedShiftCount) : IAgentTriggerEvent
{
    /// <summary>
    /// The one and only dedup key of this kind, exposed so the detector's fingerprint scan can build
    /// the identical key without constructing the event.
    /// </summary>
    public const string InstallationDedupKey = "UngroupedShiftsExist";

    private const string CountParam = "count";

    public string Kind => AgentTriggerKinds.UngroupedShifts;

    public string Severity => AgentTriggerSeverity.Low;

    /// <summary>
    /// Admins only, like the ungrouped-workforce recommendation it sits next to: acting on it means
    /// attaching duties to groups, which a planner without group rights cannot do. Setting an audience
    /// gate without a TargetUserId is also what makes the event ledger-tracked
    /// (AgentConditionLedgerPolicy.IsLedgerTracked), so the finding resolves itself once the duties are
    /// assigned instead of being re-sent.
    /// </summary>
    public bool AdminOnly => true;

    public string Summary =>
        ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.UngroupedShifts;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        [CountParam] = UngroupedShiftCount.ToString(CultureInfo.InvariantCulture)
    };

    public string DedupKey => InstallationDedupKey;

    /// <summary>
    /// No group: the finding is precisely that these duties belong to none, so scoping the event to one
    /// would be a claim the data does not carry.
    /// </summary>
    public Guid? GroupId => null;

    /// <summary>
    /// The duty list, not a single duty: several are concerned at once and there is no id to hand over.
    /// AgentConditionActionRoutes keeps a separate copy of this route for ledger rows, which stays exact
    /// only because this value is a per-kind constant.
    /// </summary>
    public string? ActionRoute => ProactiveActionRoutes.ShiftList;

    public IReadOnlyDictionary<string, string>? ActionParams => null;

    /// <summary>
    /// Keyed after the sentence's own placeholder, not after the field: ProactiveContentParamMerge
    /// renders the inbox row and the reminder from the ledger row's CURRENT payload scalars, so a
    /// differently named key would leave both showing the figure of the day the finding was opened.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        [CountParam] = UngroupedShiftCount
    };
}
