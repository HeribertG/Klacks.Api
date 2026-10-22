// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// IGoalSignalSource backed by the persisted proactive trigger history (agent_trigger_dispatches).
/// Aggregates dispatch rows from the last LookbackDays per (UserId, TriggerKind) into one GoalSignal
/// each, carrying how often the trigger fired and its first/last occurrence in that window. The
/// summary is built from GoalTypeCatalog's plain-language description, never from the trigger kind
/// itself: the identifier used to reach the summary must not appear inside it, because the reflection
/// LLM would otherwise repeat it and it would end up in a proposal a user reads. A trigger kind with
/// no catalogue entry is skipped rather than described by its identifier.
///
/// open_order is measured differently (owner decision 2026-10-22): since it is one summary per planner and day
/// (plus a further one only when the count rises), the number of messages says nothing. Its occurrence count is the
/// number of distinct company days in the window on which the planner was told about open orders - read from the
/// summary rows' dedup keys, which start with the company day (RisingCountDedupKey) - and a signal is only raised
/// from OpenOrderMinimumReportedDays days on. Three of seven days means the backlog recurs on almost half of the week
/// rather than after a single import day or a weekend; fewer would propose a goal after any ordinary import, more
/// would only fire on a permanent backlog and come too late to be a nudge. Rows of the former per-order shape carry
/// no company day in their key and are not counted. Only the last LookbackDays company days (today included, by the
/// company's own calendar from ICompanyClock) count, so the measure can never exceed LookbackDays even when the UTC
/// row window touches an eighth company day.
/// </summary>
/// <param name="dispatchRepository">Read-only access to the trigger dispatch history.</param>
/// <param name="companyClock">The company's "today", which bounds the counted open-order days.</param>
/// <param name="logger">Structured log of how many signals were collected per cycle.</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Reflection;

public class TriggerHistoryGoalSignalSource : IGoalSignalSource
{
    private const int LookbackDays = 7;
    private const int MaxDispatchRows = 2000;
    private const int OpenOrderMinimumReportedDays = 3;

    // CuriosityQuestion is Klacksy's own generated output — feeding it back into reflection would
    // recreate the self-referential loop the design spec calls out as Risk 1. MuteSuggestion is
    // meta-noise about the trigger system itself, excluded here for the same reason
    // DismissStreakEvaluator excludes it from dismiss-streak evaluation. DailyDigest is likewise
    // Klacksy's own aggregating output rather than a discoverable business-signal class: it fires once
    // per planner every day by design, not because of a recurring problem, so reflecting on "reduce daily
    // digests" would be meaningless. ScenarioPrepared is the same category: it announces what Klacksy
    // itself just did, so a goal derived from it would say "prepare fewer remediations" - a target on
    // Klacksy's own helpfulness rather than on any business condition. PeriodAutoClose is the same again:
    // it reports what Klacksy's own period close did or withheld; the business signal behind it - a period
    // that is due or overdue - is already reflected through period_close_due and period_overdue.
    // GroupingFeasibility is Klacksy's daily grouping report; its business signals have kinds of their own
    // (ungrouped_shifts, ungrouped_workforce, unstaffed_shift).
    // Explain-offer counter rows (kinds below ExplainOfferTopics.TriggerKindPrefix) measure how often Klacksy offered an
    // explanation, not a business condition, and the kind set is open-ended, so they are excluded by prefix.
    private static readonly HashSet<string> ExcludedTriggerKinds = new(StringComparer.Ordinal)
    {
        AgentTriggerKinds.CuriosityQuestion,
        AgentTriggerKinds.MuteSuggestion,
        AgentTriggerKinds.DailyDigest,
        AgentTriggerKinds.ScenarioPrepared,
        AgentTriggerKinds.KlacksyLearnedDigest,
        AgentTriggerKinds.PeriodAutoClose,
        AgentTriggerKinds.GroupingFeasibility
    };

    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;
    private readonly ICompanyClock _companyClock;
    private readonly ILogger<TriggerHistoryGoalSignalSource> _logger;

    public TriggerHistoryGoalSignalSource(
        IProactiveTriggerDispatchRepository dispatchRepository,
        ICompanyClock companyClock,
        ILogger<TriggerHistoryGoalSignalSource> logger)
    {
        _dispatchRepository = dispatchRepository;
        _companyClock = companyClock;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GoalSignal>> CollectAsync(CancellationToken cancellationToken = default)
    {
        var sinceUtc = DateTime.UtcNow.AddDays(-LookbackDays);
        var rows = await _dispatchRepository.GetSinceAsync(sinceUtc, MaxDispatchRows, cancellationToken);
        var companyToday = await _companyClock.GetTodayDateAsync(cancellationToken);

        if (rows.Count == MaxDispatchRows)
        {
            _logger.LogWarning(
                "TriggerHistoryGoalSignalSource hit the {MaxDispatchRows}-row cap — the {LookbackDays}-day window " +
                "was truncated to the newest rows, so OccurrenceCount/FirstSeenUtc may be understated and some " +
                "users may be missing entirely from this cycle",
                MaxDispatchRows, LookbackDays);
        }

        var groups = rows
            .Where(r => r.CreateTime.HasValue
                        && !string.IsNullOrWhiteSpace(r.UserId)
                        && !ExcludedTriggerKinds.Contains(r.TriggerKind)
                        && !ExplainOfferTopics.IsOfferKind(r.TriggerKind)
                        && !UiExplainOfferTopics.IsOfferKind(r.TriggerKind))
            .GroupBy(r => (r.UserId, r.TriggerKind))
            .ToList();

        var uncatalogued = groups
            .Select(g => g.Key.TriggerKind)
            .Where(kind => GoalTypeCatalog.Find(kind) == null)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (uncatalogued.Count > 0)
        {
            _logger.LogWarning(
                "TriggerHistoryGoalSignalSource skipped {Count} trigger kind(s) without a GoalTypeCatalog entry: " +
                "{Kinds}. Add them to the catalogue (with their frontend i18n keys) to make them proposable",
                uncatalogued.Count, string.Join(", ", uncatalogued));
        }

        var signals = groups
            .Select(g =>
            {
                var definition = GoalTypeCatalog.Find(g.Key.TriggerKind);
                if (definition == null)
                {
                    return null;
                }

                var timestamps = g.Select(r => r.CreateTime!.Value).ToList();
                var isOpenOrder = IsOpenOrder(g.Key.TriggerKind);
                var count = isOpenOrder ? CountReportedCompanyDays(g, companyToday) : timestamps.Count;
                if (isOpenOrder && count < OpenOrderMinimumReportedDays)
                {
                    return null;
                }

                return new GoalSignal(
                    g.Key.UserId,
                    g.Key.TriggerKind,
                    isOpenOrder ? BuildReportedDaysSummary(definition, count) : BuildSummary(definition, count),
                    count,
                    timestamps.Min(),
                    timestamps.Max(),
                    LookbackDays);
            })
            .OfType<GoalSignal>()
            .ToList();

        _logger.LogDebug(
            "TriggerHistoryGoalSignalSource collected {SignalCount} signal(s) from {RowCount} dispatch row(s) over the last {LookbackDays} day(s)",
            signals.Count, rows.Count, LookbackDays);

        return signals;
    }

    private static bool IsOpenOrder(string triggerKind) =>
        string.Equals(triggerKind, AgentTriggerKinds.OpenOrder, StringComparison.Ordinal);

    private static int CountReportedCompanyDays(IEnumerable<ProactiveTriggerDispatchRow> rows, DateOnly companyToday)
    {
        var firstCountedDay = companyToday.AddDays(1 - LookbackDays);
        var reportedDays = new HashSet<DateOnly>();
        foreach (var row in rows)
        {
            var baseKey = RisingCountDedupKey.BaseKeyOf(row.DedupKey);
            if (baseKey != null
                && DateOnly.TryParseExact(baseKey, DateTimeFormats.IsoDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
                && day >= firstCountedDay
                && day <= companyToday)
            {
                reportedDays.Add(day);
            }
        }

        return reportedDays.Count;
    }

    private static string BuildReportedDaysSummary(GoalTypeDefinition definition, int reportedDays) =>
        $"Reported on {reportedDays} of the last {LookbackDays} days: {definition.SignalDescription}.";

    private static string BuildSummary(GoalTypeDefinition definition, int occurrenceCount) =>
        $"Noticed {occurrenceCount} time(s) in the last {LookbackDays} days: {definition.SignalDescription}.";
}
