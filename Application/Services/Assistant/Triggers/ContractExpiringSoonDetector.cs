// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects ClientContract rows whose UntilDate falls inside the next 30 days and where no
/// follow-up contract is registered for the same client starting on or after the expiry.
/// Emits one ContractExpiringSoonTriggerEvent per expiring contract.
/// </summary>
/// <param name="contractRepository">Read-only client-contract scans.</param>
/// <param name="logger">Structured log per tick.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class ContractExpiringSoonDetector : IAgentTriggerDetector
{
    private const int HorizonDays = 30;

    private readonly IClientContractReadRepository _contractRepository;
    private readonly ILogger<ContractExpiringSoonDetector> _logger;

    public ContractExpiringSoonDetector(
        IClientContractReadRepository contractRepository,
        ILogger<ContractExpiringSoonDetector> logger)
    {
        _contractRepository = contractRepository;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.ContractExpiringSoon;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = today.AddDays(HorizonDays);
        var expiring = await _contractRepository.GetExpiringBetweenAsync(today, horizon, cancellationToken);
        if (expiring.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var events = new List<IAgentTriggerEvent>();
        foreach (var contract in expiring)
        {
            if (!contract.UntilDate.HasValue) continue;

            var hasFollowUp = await HasFollowUpAsync(contract, cancellationToken);
            if (hasFollowUp) continue;

            var clientName = contract.Client != null
                ? $"{contract.Client.FirstName} {contract.Client.Name}".Trim()
                : contract.ClientId.ToString();

            var daysUntilExpiry = contract.UntilDate.Value.DayNumber - today.DayNumber;

            events.Add(new ContractExpiringSoonTriggerEvent(
                contract.Id,
                contract.ClientId,
                clientName,
                contract.UntilDate.Value,
                daysUntilExpiry));
        }

        _logger.LogInformation(
            "ContractExpiringSoon scan: {Total} expiring, {Events} event(s) emitted (no follow-up)",
            expiring.Count, events.Count);

        return events;
    }

    private async Task<bool> HasFollowUpAsync(Klacks.Api.Domain.Models.Staffs.ClientContract contract, CancellationToken cancellationToken)
    {
        if (!contract.UntilDate.HasValue) return false;
        var allForClient = await _contractRepository.GetContractsForClientAsync(contract.ClientId, cancellationToken);
        return allForClient.Any(c => c.Id != contract.Id && c.FromDate >= contract.UntilDate.Value);
    }
}
