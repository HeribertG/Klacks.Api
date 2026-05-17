// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Scans the current calendar month for clients whose accumulated hours diverge from their
/// guaranteed hours by more than DriftThresholdHours. Emits one TargetHoursDriftTriggerEvent
/// per affected client; severity is set per absolute drift magnitude in the event record itself.
/// Uses IClientRepository.GetActiveClientsWithAddressesAsync to enumerate the workforce and
/// IWorkRepository.GetPeriodHoursForClients for the bulk hours read.
/// </summary>
/// <param name="clientRepository">Active client roster.</param>
/// <param name="workRepository">Bulk period-hours read with GuaranteedHours per client.</param>
/// <param name="logger">Structured log per tick.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class TargetHoursDriftDetector : IAgentTriggerDetector
{
    private const decimal DriftThresholdHours = 12m;

    private readonly IClientRepository _clientRepository;
    private readonly IWorkRepository _workRepository;
    private readonly ILogger<TargetHoursDriftDetector> _logger;

    public TargetHoursDriftDetector(
        IClientRepository clientRepository,
        IWorkRepository workRepository,
        ILogger<TargetHoursDriftDetector> logger)
    {
        _clientRepository = clientRepository;
        _workRepository = workRepository;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.TargetHoursDrift;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var periodStart = new DateOnly(today.Year, today.Month, 1);
        var periodEnd = new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        var periodLabel = $"{today.Year:0000}-{today.Month:00}";

        var clients = await _clientRepository.GetActiveClientsWithAddressesAsync(cancellationToken);
        if (clients.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var clientIds = clients.Select(c => c.Id).ToList();
        var hoursMap = await _workRepository.GetPeriodHoursForClients(clientIds, periodStart, periodEnd, analyseToken: null, cancellationToken);

        var events = new List<IAgentTriggerEvent>();
        foreach (var client in clients)
        {
            if (!hoursMap.TryGetValue(client.Id, out var hours)) continue;
            if (hours.GuaranteedHours <= 0) continue;

            var drift = hours.Hours - hours.GuaranteedHours;
            if (Math.Abs(drift) < DriftThresholdHours) continue;

            var clientName = $"{client.FirstName} {client.Name}".Trim();
            events.Add(new TargetHoursDriftTriggerEvent(
                client.Id,
                string.IsNullOrEmpty(clientName) ? client.Id.ToString() : clientName,
                drift,
                periodLabel));
        }

        _logger.LogInformation(
            "TargetHoursDrift scan: {Clients} client(s) scanned, {Events} drift event(s) emitted",
            clients.Count, events.Count);

        return events;
    }
}
