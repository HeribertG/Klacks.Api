// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects AnalyseScenario rows whose create_time is older than 48 hours and that have not
/// been accepted or rejected yet. Body pending — needs IAnalyseScenarioRepository read.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class ScenarioPendingDetector : IAgentTriggerDetector
{
    private readonly ILogger<ScenarioPendingDetector> _logger;

    public ScenarioPendingDetector(ILogger<ScenarioPendingDetector> logger)
    {
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.ScenarioPending;

    public Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ScenarioPending scan tick — detector body pending");
        return Task.FromResult<IReadOnlyList<IAgentTriggerEvent>>(Array.Empty<IAgentTriggerEvent>());
    }
}
