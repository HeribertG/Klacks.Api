// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detector that watches for Work rows whose lock_level conflicts with a recent
/// wizard / harmonizer run. Today returns an empty list — the actual scan
/// (correlate agent_skill_executions.expected_diff vs. lock_level) is the next
/// iteration's job; the pipeline is fully wired end-to-end so the body can drop in.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class LockConflictDetector : IAgentTriggerDetector
{
    private readonly ILogger<LockConflictDetector> _logger;

    public LockConflictDetector(ILogger<LockConflictDetector> logger)
    {
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.LockConflict;

    public Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("LockConflict scan tick — detector body pending (S8 backlog)");
        return Task.FromResult<IReadOnlyList<IAgentTriggerEvent>>(Array.Empty<IAgentTriggerEvent>());
    }
}
