// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detector that scans the next 7 days for shift assignments that lack a corresponding Work
/// and emits UnstaffedShiftTriggerEvent for each. Today returns an empty list — the actual
/// schedule scan (IShiftScheduleRepository + IWorkRepository) is the next iteration's job;
/// the pipeline (service + background service + rate limiter + hub) is wired end-to-end
/// so the detector body can drop in without touching the rest.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class UnstaffedShift7dDetector : IAgentTriggerDetector
{
    private readonly ILogger<UnstaffedShift7dDetector> _logger;

    public UnstaffedShift7dDetector(ILogger<UnstaffedShift7dDetector> logger)
    {
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.UnstaffedShift;

    public Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("UnstaffedShift7d scan tick — detector body pending (S8 backlog)");
        return Task.FromResult<IReadOnlyList<IAgentTriggerEvent>>(Array.Empty<IAgentTriggerEvent>());
    }
}
