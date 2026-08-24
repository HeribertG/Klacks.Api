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
using Klacks.Api.Domain.DTOs.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class ClientMissingCoreDataDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    public const int MaxFindingsPerTick = 25;

    private const int UncappedResultCount = int.MaxValue;

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
        var statuses = await _coreDataReadRepository.GetActiveClientsWithMissingCoreDataAsync(
            Today(), MaxFindingsPerTick, cancellationToken);
        if (statuses.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var events = new List<IAgentTriggerEvent>();
        foreach (var status in statuses)
        {
            var displayName = DisplayName(status);

            foreach (var missingField in MissingFields(status))
            {
                if (events.Count >= MaxFindingsPerTick) break;

                events.Add(new ClientMissingCoreDataTriggerEvent(status.ClientId, displayName, missingField));
            }
        }

        _logger.LogInformation(
            "ClientMissingCoreData scan: {Clients} client(s) with gaps, {Events} event(s) emitted",
            statuses.Count, events.Count);

        return events;
    }

    /// <summary>
    /// Calls the very same repository method for the very same reference date, only without the result
    /// cap, and derives the missing fields through the same MissingFields mapping DetectAsync uses - so
    /// a client with two gaps yields both fingerprints here exactly as it yields two events there.
    /// </summary>
    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await _coreDataReadRepository.GetActiveClientsWithMissingCoreDataAsync(
            Today(), UncappedResultCount, cancellationToken);

        return statuses
            .SelectMany(status => MissingFields(status)
                .Select(missingField => AgentConditionLedgerPolicy.FingerprintFor(
                    Kind,
                    ClientMissingCoreDataTriggerEvent.DedupKeyFor(status.ClientId, missingField))))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static IEnumerable<string> MissingFields(ClientCoreDataStatus status)
    {
        if (!status.HasActiveAddress)
        {
            yield return ClientMissingCoreDataTriggerEvent.AddressField;
        }

        if (!status.HasEmailOrPhone)
        {
            yield return ClientMissingCoreDataTriggerEvent.ContactField;
        }
    }

    private static string DisplayName(ClientCoreDataStatus status)
    {
        var clientName = $"{status.FirstName} {status.Name}".Trim();

        return string.IsNullOrEmpty(clientName) ? status.ClientId.ToString() : clientName;
    }

    private DateOnly Today() => DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
}
