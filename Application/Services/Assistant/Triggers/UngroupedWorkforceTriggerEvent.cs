// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when an installation is already being planned and carries a workforce of at least
/// UngroupedWorkforceDetector.MinEmployeesForGroupingSuggestion active employees, yet holds no group at
/// all. A quiet recommendation rather than an alert: Severity Low keeps it in the inbox and the badge
/// and out of the live push, because nothing is broken — the installation simply cannot scope, filter
/// or hand over any part of its workforce yet.
///
/// The sentence must never suggest that creating a group takes visibility away from anybody.
/// GroupVisibilityPreservationService preserves the status quo when the first group appears, so such a
/// claim would be factually wrong as well as a reason not to follow the recommendation.
///
/// The DedupKey is a CONSTANT and deliberately carries no count. Dispatch dedup has no time window
/// (ProactiveTriggerDispatchRepository.WasDispatchedAsync), so an administrator sees this once — ever,
/// not once per day — which is the whole point of a recommendation about a standing setup fact. A count
/// in the key would additionally open a new ledger row on every tick that hires or loses one person,
/// while the row it replaced would never resolve.
/// </summary>
/// <param name="ActiveEmployeeCount">Employees on the books the recommendation is sized against.</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record UngroupedWorkforceTriggerEvent(int ActiveEmployeeCount) : IAgentTriggerEvent
{
    /// <summary>
    /// The one and only dedup key of this kind, exposed so the detector's fingerprint scan can build
    /// the identical key without constructing the event.
    /// </summary>
    public const string InstallationDedupKey = "NoGroupsAtAll";

    private const string CountParam = "count";

    public string Kind => AgentTriggerKinds.UngroupedWorkforce;

    public string Severity => AgentTriggerSeverity.Low;

    /// <summary>
    /// Admins only, narrower than PlannersOnly: the recommendation leads to creating a group, and a
    /// planner who may not create one would be handed an offer they cannot take. Setting an audience
    /// gate without a TargetUserId is also what makes the event ledger-tracked
    /// (AgentConditionLedgerPolicy.IsLedgerTracked), so the finding resolves itself once a group exists
    /// instead of being re-sent. Harmonising the rights model is a separate piece of work.
    /// </summary>
    public bool AdminOnly => true;

    public string Summary =>
        ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.UngroupedWorkforce;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        [CountParam] = ActiveEmployeeCount.ToString(CultureInfo.InvariantCulture)
    };

    public string DedupKey => InstallationDedupKey;

    public Guid? GroupId => null;

    /// <summary>
    /// The group list, not a single group: the finding is precisely that no group exists, so there is
    /// nothing to preselect. AgentConditionActionRoutes keeps a separate copy of this route for ledger
    /// rows, which stays exact only because this value is a per-kind constant.
    /// </summary>
    public string? ActionRoute => ProactiveActionRoutes.GroupList;

    public IReadOnlyDictionary<string, string>? ActionParams => null;

    /// <summary>
    /// Keyed after the sentence's own placeholder, not after the field: ProactiveContentParamMerge
    /// renders the inbox row and the reminder from the ledger row's CURRENT payload scalars, so a
    /// differently named key would leave both showing the headcount of the day the finding was opened.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        [CountParam] = ActiveEmployeeCount
    };
}
