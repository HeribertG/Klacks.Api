// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects groups whose current pay period ends within 3 days but is still open (no
/// SealedDay marker). Body pending — needs ISealedDayRepository / IGroupRepository join.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class PeriodCloseDueDetector : IAgentTriggerDetector
{
    private readonly ILogger<PeriodCloseDueDetector> _logger;

    public PeriodCloseDueDetector(ILogger<PeriodCloseDueDetector> logger)
    {
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.PeriodCloseDue;

    public Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("PeriodCloseDue scan tick — detector body pending");
        return Task.FromResult<IReadOnlyList<IAgentTriggerEvent>>(Array.Empty<IAgentTriggerEvent>());
    }
}
