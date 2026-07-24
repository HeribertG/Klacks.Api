// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects active clients (membership valid today) lacking core data and emits one
/// ClientMissingCoreDataTriggerEvent per client and missing field: address when no active
/// address exists, contact when neither an e-mail nor a phone communication entry exists.
/// Emission is capped at MaxFindingsPerTick events per tick.
/// </summary>
/// <param name="coreDataReadRepository">Read-only core-data quality scans.</param>
/// <param name="logger">Structured log per tick.</param>
/// <param name="timeProvider">Clock used to derive the reference date.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class ClientMissingCoreDataDetector : IAgentTriggerDetector
{
    public const int MaxFindingsPerTick = 25;

    private readonly IClientCoreDataReadRepository _coreDataReadRepository;
    private readonly ILogger<ClientMissingCoreDataDetector> _logger;
    private readonly TimeProvider _timeProvider;

    public ClientMissingCoreDataDetector(
        IClientCoreDataReadRepository coreDataReadRepository,
        ILogger<ClientMissingCoreDataDetector> logger,
        TimeProvider timeProvider)
    {
        _coreDataReadRepository = coreDataReadRepository;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public string Kind => AgentTriggerKinds.ClientMissingCoreData;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var statuses = await _coreDataReadRepository.GetActiveClientsWithMissingCoreDataAsync(
            today, MaxFindingsPerTick, cancellationToken);
        if (statuses.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var events = new List<IAgentTriggerEvent>();
        foreach (var status in statuses)
        {
            if (events.Count >= MaxFindingsPerTick) break;

            var clientName = $"{status.FirstName} {status.Name}".Trim();
            var displayName = string.IsNullOrEmpty(clientName) ? status.ClientId.ToString() : clientName;

            if (!status.HasActiveAddress)
            {
                events.Add(new ClientMissingCoreDataTriggerEvent(
                    status.ClientId, displayName, ClientMissingCoreDataTriggerEvent.AddressField));
            }

            if (!status.HasEmailOrPhone && events.Count < MaxFindingsPerTick)
            {
                events.Add(new ClientMissingCoreDataTriggerEvent(
                    status.ClientId, displayName, ClientMissingCoreDataTriggerEvent.ContactField));
            }
        }

        _logger.LogInformation(
            "ClientMissingCoreData scan: {Clients} client(s) with gaps, {Events} event(s) emitted",
            statuses.Count, events.Count);

        return events;
    }
}
