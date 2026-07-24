// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects plannable clients (membership valid inside the window) without a single
/// availability entry for the next calendar month and emits one AvailabilityGapTriggerEvent
/// per client, capped at MaxFindingsPerTick. Stays silent while no availability entries
/// exist in the system at all, so installations not using the feature never get spammed.
/// </summary>
/// <param name="availabilityReadRepository">Read-only availability-gap scans.</param>
/// <param name="logger">Structured log per tick.</param>
/// <param name="timeProvider">Clock used to derive today and the next-month window.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class AvailabilityGapDetector : IAgentTriggerDetector
{
    public const int MaxFindingsPerTick = 25;

    private readonly IClientAvailabilityReadRepository _availabilityReadRepository;
    private readonly ILogger<AvailabilityGapDetector> _logger;
    private readonly TimeProvider _timeProvider;

    public AvailabilityGapDetector(
        IClientAvailabilityReadRepository availabilityReadRepository,
        ILogger<AvailabilityGapDetector> logger,
        TimeProvider timeProvider)
    {
        _availabilityReadRepository = availabilityReadRepository;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public string Kind => AgentTriggerKinds.AvailabilityGap;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var monthStart = new DateOnly(today.Year, today.Month, 1).AddMonths(1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        if (!await _availabilityReadRepository.AnyAvailabilityEntriesExistAsync(cancellationToken))
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var clients = await _availabilityReadRepository.GetPlannableClientsWithoutAvailabilityAsync(
            monthStart, monthEnd, MaxFindingsPerTick, cancellationToken);
        if (clients.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var daysUntilPeriodStart = monthStart.DayNumber - today.DayNumber;
        var events = new List<IAgentTriggerEvent>();
        foreach (var client in clients.Take(MaxFindingsPerTick))
        {
            var clientName = $"{client.FirstName} {client.Name}".Trim();
            events.Add(new AvailabilityGapTriggerEvent(
                client.ClientId,
                string.IsNullOrEmpty(clientName) ? client.ClientId.ToString() : clientName,
                monthStart,
                monthEnd,
                daysUntilPeriodStart));
        }

        _logger.LogInformation(
            "AvailabilityGap scan: {Clients} client(s) without availability for {Month}, {Events} event(s) emitted",
            clients.Count, $"{monthStart:yyyy-MM}", events.Count);

        return events;
    }
}
