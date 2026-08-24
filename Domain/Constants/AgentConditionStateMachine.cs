// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The condition-ledger lifecycle expressed as data: which statuses count as open and which status
/// transitions are legal. Three consumers must agree on this and therefore share it: the ledger service
/// rejects any transition missing from <see cref="AllowedTransitions"/>, the repository's "open row"
/// queries filter on <see cref="TerminalStatuses"/>, and AgentConditionConfiguration builds the partial
/// unique index filter on Fingerprint from the same set. Drift between the index filter and the query
/// would break the duplicate-insert path that makes re-arm safe across API instances, so the set is
/// defined once here rather than restated per call site.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class AgentConditionStateMachine
{
    /// <summary>
    /// Statuses a row can never leave again. A row in one of these no longer blocks the partial unique
    /// index on Fingerprint, which is exactly what lets a re-arm insert a fresh row for the same
    /// fingerprint instead of reopening history.
    /// </summary>
    public static readonly IReadOnlyList<AgentConditionStatus> TerminalStatuses =
    [
        AgentConditionStatus.Executed,
        AgentConditionStatus.Rejected,
        AgentConditionStatus.Resolved,
        AgentConditionStatus.Escalated
    ];

    /// <summary>
    /// The complement of <see cref="TerminalStatuses"/>: a row still under way. Derived rather than listed,
    /// so an eighth status added to the enum lands here automatically instead of leaving a second, silently
    /// stale copy of the same rule.
    ///
    /// This is "open" in the ledger's sense - the row still blocks a re-arm insert - which is NOT the same
    /// as "a finding still worth showing a planner". Escalated is terminal here, deliberately, so a
    /// re-detected fingerprint can open a fresh row; but the Etappe 3f list_open_findings skill is specified
    /// over Detected, Reported, Prepared AND Escalated. That skill must therefore state its own status set
    /// rather than reuse this one, or it will silently hide exactly the findings that most need attention.
    /// </summary>
    public static readonly IReadOnlyList<AgentConditionStatus> OpenStatuses =
        Enum.GetValues<AgentConditionStatus>()
            .Where(status => !TerminalStatuses.Contains(status))
            .ToArray();

    /// <summary>
    /// Legal target statuses per source status. Deliberately fail-closed: anything not listed here is a
    /// programming error, not a runtime outcome. Resolved is reachable from every open status because a
    /// condition can disappear at any point in its life; Executed is reachable only from Prepared because
    /// execution always acts on a prepared remediation; Rejected needs a human, who can only have been
    /// told about the finding once it was Reported; Escalated follows failed remediation attempts, which
    /// can only accrue after a Reported row was claimed for handling.
    /// </summary>
    public static readonly IReadOnlyDictionary<AgentConditionStatus, IReadOnlyList<AgentConditionStatus>> AllowedTransitions =
        new Dictionary<AgentConditionStatus, IReadOnlyList<AgentConditionStatus>>
        {
            [AgentConditionStatus.Detected] =
            [
                AgentConditionStatus.Reported,
                AgentConditionStatus.Resolved
            ],
            [AgentConditionStatus.Reported] =
            [
                AgentConditionStatus.Prepared,
                AgentConditionStatus.Rejected,
                AgentConditionStatus.Resolved,
                AgentConditionStatus.Escalated
            ],
            [AgentConditionStatus.Prepared] =
            [
                AgentConditionStatus.Executed,
                AgentConditionStatus.Rejected,
                AgentConditionStatus.Resolved,
                AgentConditionStatus.Escalated
            ],
            [AgentConditionStatus.Executed] = [],
            [AgentConditionStatus.Rejected] = [],
            [AgentConditionStatus.Resolved] = [],
            [AgentConditionStatus.Escalated] = []
        };

    public static bool IsOpen(AgentConditionStatus status) => !TerminalStatuses.Contains(status);

    public static bool IsLegalTransition(AgentConditionStatus fromStatus, AgentConditionStatus toStatus) =>
        AllowedTransitions.TryGetValue(fromStatus, out var allowed) && allowed.Contains(toStatus);
}
