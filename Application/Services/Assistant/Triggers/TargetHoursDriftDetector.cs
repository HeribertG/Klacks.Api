// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Scans recently closed pay periods for clients whose accumulated hours diverged from target
/// by more than the configured threshold. Body returns an empty list today — the period-hours
/// aggregation join lives in IPeriodHourRepository and is the next iteration's work.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class TargetHoursDriftDetector : IAgentTriggerDetector
{
    private readonly ILogger<TargetHoursDriftDetector> _logger;

    public TargetHoursDriftDetector(ILogger<TargetHoursDriftDetector> logger)
    {
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.TargetHoursDrift;

    public Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("TargetHoursDrift scan tick — detector body pending");
        return Task.FromResult<IReadOnlyList<IAgentTriggerEvent>>(Array.Empty<IAgentTriggerEvent>());
    }
}
