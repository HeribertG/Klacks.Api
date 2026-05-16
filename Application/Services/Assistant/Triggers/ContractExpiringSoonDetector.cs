// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects ClientContract rows whose ValidUntil is within 30 days and where no follow-up
/// contract has been registered. Body pending — needs IContractRepository sequence-scan.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class ContractExpiringSoonDetector : IAgentTriggerDetector
{
    private readonly ILogger<ContractExpiringSoonDetector> _logger;

    public ContractExpiringSoonDetector(ILogger<ContractExpiringSoonDetector> logger)
    {
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.ContractExpiringSoon;

    public Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ContractExpiringSoon scan tick — detector body pending");
        return Task.FromResult<IReadOnlyList<IAgentTriggerEvent>>(Array.Empty<IAgentTriggerEvent>());
    }
}
