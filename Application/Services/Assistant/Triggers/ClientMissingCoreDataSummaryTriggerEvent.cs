// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired once per scan and per missing core-data field when at least one active employee lacks it.
/// Deliberately ONE event for all employees sharing a gap rather than one per employee, which
/// produced one inbox row per employee per recipient.
///
/// Aggregated per FIELD and not into a single event, because the two gaps are two different
/// statements with two different severities (a missing address is Medium, a missing way to be
/// contacted is Low) and two long-standing i18n sentences. Merging them would force an invented
/// sentence and an invented severity, and the planner could no longer tell what is actually missing.
/// At most two events per tick is the whole reduction that is needed.
///
/// The dedup key is the field alone - deliberately not the company day. This finding has no natural
/// period, and a day-stamped key would leave the active fingerprint set every midnight even though
/// nothing was fixed, so reconcile would resolve the row and the partial unique index would re-arm a
/// fresh one the next morning: a permanent daily nag where the per-employee shape used to fall silent
/// after the first report. Re-reminding a standing condition is the reminder sweep's job
/// (ProactiveReminderService), not the dedup key's. A constant key also preserves the cadence this kind
/// has today, which is "once per gap, ever, until it is filled" - the per-employee key carries no date
/// either.
///
/// ACCEPTED COST of that choice, and what it is NOT: the row is announced once. WasDispatchedAsync keys
/// on (user, kind, dedupKey, conditionId) with no time window and ContentParamsJson is written once, so
/// the twenty-eight employees who join the gap next month never trigger a second notification until the
/// last gap is filled and the row re-arms. What they no longer cost is a WRONG number: the condition's
/// PayloadJson is refreshed by TouchAsync on every tick, and both the reminder sweep and the inbox
/// re-render the sentence from it (ProactiveContentParamMerge), so the standing row states forty rather
/// than the twelve it was opened with. Should a repeated ANNOUNCEMENT matter more than the unchanged
/// cadence, the one-line change is DedupKeyFor(missingField, companyDay) spelling
/// "{missingField}:{yyyy-MM}" - a monthly refresh exactly like TargetHoursDrift, still not a day key.
/// That is an owner decision, because it adds a recurring notification this kind does not have today.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record ClientMissingCoreDataSummaryTriggerEvent(
    IReadOnlyList<ProactiveAffectedClient> AffectedClients,
    string MissingField) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.ClientMissingCoreData;

    public string Severity => IsAddressGap
        ? AgentTriggerSeverity.Medium
        : AgentTriggerSeverity.Low;

    public bool PlannersOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + (IsAddressGap
        ? ProactiveMessageI18nKeys.ClientMissingAddressSummary
        : ProactiveMessageI18nKeys.ClientMissingContactSummary);

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["count"] = AffectedClients.Count.ToString(CultureInfo.InvariantCulture),
        ["names"] = ProactiveNameListRenderer.Render(
            AffectedClients.Select(client => client.ClientName).ToList())
    };

    public string DedupKey => DedupKeyFor(MissingField);

    /// <summary>
    /// The DedupKey spelling as a function of its key field, so ClientMissingCoreDataDetector's
    /// fingerprint scan can build the identical key without constructing the event.
    /// </summary>
    public static string DedupKeyFor(string missingField) => missingField;

    /// <summary>
    /// The employee list, not the edit page: ProactiveActionRoutes.ClientEdit resolves to
    /// /workplace/edit-address, whose component reads the route parameter id and falls back to
    /// createClient() when it is absent. The per-employee shape already opened an empty new-employee
    /// form because its clientId travelled as a query parameter the component ignores; an aggregate
    /// naming up to ten people has no single id to pass at all.
    /// </summary>
    public string? ActionRoute => ProactiveActionRoutes.ClientList;

    /// <summary>
    /// "names" carries the RENDERED list beside the structured "clients" rows, deliberately redundant:
    /// the ledger refreshes PayloadJson on every detector tick and both the reminder sweep and the inbox
    /// re-render their sentence from its scalar entries (ProactiveContentParamMerge), which a nested
    /// array is not. Without it the sentence would state a current count next to the name list of the
    /// day the gap was first detected.
    ///
    /// "clients" is capped at the number of names the sentence can show. Nothing reads it back - the
    /// inbox and the reminder sweep both drop non-scalar payload entries - while the uncapped list was
    /// written into PayloadJson on every detector tick and loaded again on every inbox poll, and this
    /// kind's row stands open until the last gap is filled, so that list only ever grows. "count" stays
    /// the full total, so the sentence keeps telling the truth about how many people are affected.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["missingField"] = MissingField,
        ["count"] = AffectedClients.Count,
        ["names"] = ProactiveNameListRenderer.Render(
            AffectedClients.Select(client => client.ClientName).ToList()),
        ["clients"] = AffectedClients.Take(ProactiveNameListRenderer.MaxListedNames).ToList()
    };

    private bool IsAddressGap =>
        string.Equals(MissingField, ClientMissingCoreDataTriggerEvent.AddressField, StringComparison.Ordinal);
}
