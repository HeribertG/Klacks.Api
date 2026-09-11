// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects plannable clients (membership valid inside the window) without a single
/// availability entry for the next calendar month and emits one AvailabilityGapTriggerEvent
/// per client, capped at MaxFindingsPerTick. Stays silent while no availability entries
/// exist in the system at all, so installations not using the feature never get spammed.
/// </summary>
/// <param name="availabilityReadRepository">Read-only availability-gap scans.</param>
/// <param name="logger">Structured log per tick.</param>
/// <param name="companyClock">Resolves today and the next-month window in the company's own local day.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class AvailabilityGapDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    public const int MaxFindingsPerTick = 25;

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

        var clients = await _availabilityReadRepository.GetPlannableClientsWithoutAvailabilityAsync(
            window.MonthStart, window.MonthEnd, MaxFindingsPerTick, cancellationToken);
        if (clients.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var daysUntilPeriodStart = window.MonthStart.DayNumber - window.Today.DayNumber;
        var events = new List<IAgentTriggerEvent>();
        foreach (var client in clients.Take(MaxFindingsPerTick))
        {
            var clientName = $"{client.FirstName} {client.Name}".Trim();
            events.Add(new AvailabilityGapTriggerEvent(
                client.ClientId,
                string.IsNullOrEmpty(clientName) ? client.ClientId.ToString() : clientName,
                window.MonthStart,
                window.MonthEnd,
                daysUntilPeriodStart));
        }

        _logger.LogInformation(
            "AvailabilityGap scan: {Clients} client(s) without availability for {Month}, {Events} event(s) emitted",
            clients.Count, $"{window.MonthStart:yyyy-MM}", events.Count);

        return events;
    }

    /// <summary>
    /// Calls the very same repository method over the very same window, only without the result cap, so
    /// the two paths cannot drift apart in their predicates. The "no availability entry exists anywhere"
    /// gate is shared as well: when the installation does not use the feature at all, DetectAsync stays
    /// silent, and an empty fingerprint set correctly resolves whatever findings a previous, still
    /// active period had left open.
    /// </summary>
    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        var window = await BuildWindowAsync(cancellationToken);

        if (!await _availabilityReadRepository.AnyAvailabilityEntriesExistAsync(cancellationToken))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var clients = await _availabilityReadRepository.GetPlannableClientsWithoutAvailabilityAsync(
            window.MonthStart, window.MonthEnd, UncappedResultCount, cancellationToken);

        return clients
            .Select(client => AgentConditionLedgerPolicy.FingerprintFor(
                Kind,
                AvailabilityGapTriggerEvent.DedupKeyFor(client.ClientId, window.MonthStart)))
            .ToHashSet(StringComparer.Ordinal);
    }

    private async Task<(DateOnly Today, DateOnly MonthStart, DateOnly MonthEnd)> BuildWindowAsync(CancellationToken cancellationToken)
    {
        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var monthStart = new DateOnly(today.Year, today.Month, 1).AddMonths(1);

        return (today, monthStart, monthStart.AddMonths(1).AddDays(-1));
    }
}
