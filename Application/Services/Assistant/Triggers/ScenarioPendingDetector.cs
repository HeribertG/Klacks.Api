// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects AnalyseScenario rows in Active status whose CreateTime is older than 48 hours
/// and emits one ScenarioPendingTriggerEvent per such row. Severity escalates the longer
/// the scenario sits unanswered (≥72h medium, ≥168h high).
/// </summary>
/// <param name="scenarioRepository">Lists active scenarios.</param>
/// <param name="logger">Structured log per tick.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class ScenarioPendingDetector : IAgentTriggerDetector
{
    private const int MinimumPendingHours = 48;

    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly ILogger<ScenarioPendingDetector> _logger;

    public ScenarioPendingDetector(
        IAnalyseScenarioRepository scenarioRepository,
        ILogger<ScenarioPendingDetector> logger)
    {
        _scenarioRepository = scenarioRepository;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.ScenarioPending;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var scenarios = await _scenarioRepository.GetByGroupAsync(null, cancellationToken);
        if (scenarios.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var nowUtc = DateTime.UtcNow;
        var events = new List<IAgentTriggerEvent>();

        foreach (var scenario in scenarios)
        {
            if (scenario.Status != AnalyseScenarioStatus.Active) continue;
            if (!scenario.CreateTime.HasValue) continue;

            var hoursPending = (int)(nowUtc - scenario.CreateTime.Value).TotalHours;
            if (hoursPending < MinimumPendingHours) continue;

            var groupName = scenario.Group?.Name ?? "(unassigned)";

            events.Add(new ScenarioPendingTriggerEvent(
                scenario.Id,
                hoursPending,
                scenario.GroupId,
                groupName));
        }

        _logger.LogInformation(
            "ScenarioPending scan: {Total} scenario(s) scanned, {Events} pending events emitted",
            scenarios.Count, events.Count);

        return events;
    }
}
