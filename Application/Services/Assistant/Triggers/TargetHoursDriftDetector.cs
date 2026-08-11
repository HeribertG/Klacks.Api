// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Scans the last completed calendar month for clients whose accumulated hours diverge from their
/// guaranteed hours by more than DriftThresholdHours. Emits one TargetHoursDriftTriggerEvent
/// per affected client; severity is set per absolute drift magnitude in the event record itself.
/// Uses IClientRepository.GetActiveClientsWithAddressesAsync to enumerate the workforce and
/// IWorkRepository.GetPeriodHoursForClients for the bulk hours read. Customers are dropped from
/// that roster, see DetectAsync.
/// </summary>
/// <param name="clientRepository">Active client roster, customers included.</param>
/// <param name="workRepository">Bulk period-hours read with GuaranteedHours per client.</param>
/// <param name="logger">Structured log per tick.</param>
/// <param name="timeProvider">Clock used to derive the last completed month.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class TargetHoursDriftDetector : IAgentTriggerDetector
{
    private const decimal DriftThresholdHours = 12m;

    private readonly IClientRepository _clientRepository;
    private readonly IWorkRepository _workRepository;
    private readonly ILogger<TargetHoursDriftDetector> _logger;
    private readonly TimeProvider _timeProvider;

    public TargetHoursDriftDetector(
        IClientRepository clientRepository,
        IWorkRepository workRepository,
        ILogger<TargetHoursDriftDetector> logger,
        TimeProvider timeProvider)
    {
        _clientRepository = clientRepository;
        _workRepository = workRepository;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public string Kind => AgentTriggerKinds.TargetHoursDrift;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        // The running month is always short on hours simply because it has not happened yet, so
        // scanning it reports a deficit for everyone. Only the last completed month is a period
        // for which time entry is actually expected.
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var periodEnd = new DateOnly(today.Year, today.Month, 1).AddDays(-1);
        var periodStart = new DateOnly(periodEnd.Year, periodEnd.Month, 1);
        var periodLabel = $"{periodEnd.Year:0000}-{periodEnd.Month:00}";

        // GetActiveClientsWithAddressesAsync only excludes deleted rows, so it hands out customers
        // as well. A customer is never scheduled and ClientBaseQueryService keeps them out of the
        // schedule entirely, so a drift message about one leads to a page that cannot show them.
        var clients = (await _clientRepository.GetActiveClientsWithAddressesAsync(cancellationToken))
            .Where(client => client.Type != EntityTypeEnum.Customer)
            .ToList();
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

            var drift = (hours.Hours + hours.Surcharges) - hours.GuaranteedHours;
            if (Math.Abs(drift) < DriftThresholdHours) continue;

            var clientName = $"{client.FirstName} {client.Name}".Trim();
            events.Add(new TargetHoursDriftTriggerEvent(
                client.Id,
                string.IsNullOrEmpty(clientName) ? client.Id.ToString() : clientName,
                drift,
                periodLabel));
        }

        _logger.LogInformation(
            "TargetHoursDrift scan for {Period}: {Clients} client(s) scanned, {Events} drift event(s) emitted",
            periodLabel, clients.Count, events.Count);

        return events;
    }
}
