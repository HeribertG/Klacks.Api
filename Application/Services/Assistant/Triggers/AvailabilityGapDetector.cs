// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects plannable clients (membership valid inside the window) without a single availability entry
/// for the next calendar month and emits ONE aggregated AvailabilityGapSummaryTriggerEvent naming every
/// affected client, never one event per client. Stays silent while no availability entries exist in the
/// system at all, so installations not using the feature never get spammed.
/// Deliberately carries no result cap any more: the aggregate reports a count, and a capped read would
/// report the cap instead of the truth. Both paths therefore run the identical uncapped repository
/// query - which is what the fingerprint scan alone already did before the aggregation.
/// </summary>
/// <param name="availabilityReadRepository">Read-only availability-gap scans.</param>
/// <param name="logger">Structured log per tick.</param>
/// <param name="companyClock">Resolves today and the next-month window in the company's own local day.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.DTOs.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class AvailabilityGapDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    private const int UncappedResultCount = int.MaxValue;

    private readonly IClientAvailabilityReadRepository _availabilityReadRepository;
    private readonly ILogger<AvailabilityGapDetector> _logger;
    private readonly ICompanyClock _companyClock;

    public AvailabilityGapDetector(
        IClientAvailabilityReadRepository availabilityReadRepository,
        ILogger<AvailabilityGapDetector> logger,
        ICompanyClock companyClock)
    {
        _availabilityReadRepository = availabilityReadRepository;
        _logger = logger;
        _companyClock = companyClock;
    }

    public string Kind => AgentTriggerKinds.AvailabilityGap;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var window = await BuildWindowAsync(cancellationToken);

        if (!await _availabilityReadRepository.AnyAvailabilityEntriesExistAsync(cancellationToken))
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var clients = await ScanAsync(window, cancellationToken);
        if (clients.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var affected = clients
            .Select(client => new ProactiveAffectedClient(client.ClientId, DisplayName(client)))
            .ToList();

        _logger.LogInformation(
            "AvailabilityGap scan: {Clients} client(s) without availability for the month starting {Month}, one aggregated event emitted",
            affected.Count, window.MonthStart);

        return
        [
            new AvailabilityGapSummaryTriggerEvent(
                affected,
                window.MonthStart,
                window.MonthEnd,
                window.MonthStart.DayNumber - window.Today.DayNumber)
        ];
    }

    /// <summary>
    /// Runs the identical scan over the identical window and folds it to the single fingerprint the
    /// aggregate carries, so the two paths cannot drift apart in their predicates. The "no availability
    /// entry exists anywhere" gate is shared as well: when the installation does not use the feature at
    /// all, DetectAsync stays silent, and an empty fingerprint set correctly resolves whatever findings a
    /// previous, still active period had left open. Zero findings must likewise yield an EMPTY set and
    /// never the period fingerprint, or the row would stay open after the last gap was filled.
    /// </summary>
    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        var window = await BuildWindowAsync(cancellationToken);

        if (!await _availabilityReadRepository.AnyAvailabilityEntriesExistAsync(cancellationToken))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var clients = await ScanAsync(window, cancellationToken);

        return clients.Count == 0
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(
                [
                    AgentConditionLedgerPolicy.FingerprintFor(
                        Kind, AvailabilityGapSummaryTriggerEvent.DedupKeyFor(window.MonthStart))
                ],
                StringComparer.Ordinal);
    }

    private async Task<List<PlannableClientInfo>> ScanAsync(
        (DateOnly Today, DateOnly MonthStart, DateOnly MonthEnd) window,
        CancellationToken cancellationToken) =>
        await _availabilityReadRepository.GetPlannableClientsWithoutAvailabilityAsync(
            window.MonthStart, window.MonthEnd, UncappedResultCount, cancellationToken);

    private static string DisplayName(PlannableClientInfo client)
    {
        var clientName = $"{client.FirstName} {client.Name}".Trim();

        return string.IsNullOrEmpty(clientName) ? client.ClientId.ToString() : clientName;
    }

    private async Task<(DateOnly Today, DateOnly MonthStart, DateOnly MonthEnd)> BuildWindowAsync(CancellationToken cancellationToken)
    {
        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var monthStart = new DateOnly(today.Year, today.Month, 1).AddMonths(1);

        return (today, monthStart, monthStart.AddMonths(1).AddDays(-1));
    }
}
