// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Detects AnalyseScenario rows in Active status whose CreateTime is older than 48 hours but not older than
/// 30 days and emits one ScenarioPendingTriggerEvent per such row. Severity escalates the longer the scenario
/// sits unanswered (≥72h medium, ≥168h high). The status and age filters run in the database. The upper age
/// bound keeps a first deploy from flooding planners with ancient drafts nobody will answer any more.
/// A draft that an automatic next-period run already reported as blocked (an open
/// NextPeriodAutoCommitBlocked ledger row names it) is not reported a second time here. Residual: once a
/// planner dismisses that blocked row, the draft becomes eligible for this reminder again.
/// </summary>
/// <param name="scenarioRepository">Lists active scenarios inside the reporting window.</param>
/// <param name="conditionRepository">Open next-period ledger rows, read to skip drafts already reported as blocked.</param>
/// <param name="timeProvider">Source of "now" for the reporting window.</param>
/// <param name="logger">Structured log per tick.</param>

using System.Text.Json;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class ScenarioPendingDetector : IAgentTriggerDetector
{
    public const int MinimumPendingHours = 48;
    public const int MaximumPendingDays = 30;

    private const string UnassignedGroupName = "(unassigned)";

    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAgentConditionRepository _conditionRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ScenarioPendingDetector> _logger;

    public ScenarioPendingDetector(
        IAnalyseScenarioRepository scenarioRepository,
        IAgentConditionRepository conditionRepository,
        TimeProvider timeProvider,
        ILogger<ScenarioPendingDetector> logger)
    {
        _scenarioRepository = scenarioRepository;
        _conditionRepository = conditionRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.ScenarioPending;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var scenarios = await _scenarioRepository.GetActiveCreatedBetweenAsync(
            nowUtc.AddDays(-MaximumPendingDays),
            nowUtc.AddHours(-MinimumPendingHours),
            cancellationToken);
        if (scenarios.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var alreadyReportedAsBlocked = await ReadBlockedScenarioIdsAsync(cancellationToken);
        var events = new List<IAgentTriggerEvent>();

        foreach (var scenario in scenarios)
        {
            if (!scenario.CreateTime.HasValue) continue;
            if (alreadyReportedAsBlocked.Contains(scenario.Id)) continue;

            var hoursPending = (int)(nowUtc - scenario.CreateTime.Value).TotalHours;

            events.Add(new ScenarioPendingTriggerEvent(
                scenario.Id,
                hoursPending,
                scenario.GroupId,
                scenario.Group?.Name ?? UnassignedGroupName));
        }

        _logger.LogInformation(
            "ScenarioPending scan: {Total} scenario(s) in the reporting window, {Blocked} already reported as blocked, {Events} pending events emitted",
            scenarios.Count, alreadyReportedAsBlocked.Count, events.Count);

        return events;
    }

    /// <summary>
    /// Scenario ids named by open "auto-commit blocked" rows of the next-period kind. A row without a block
    /// reason (a due or started notice) or with an unreadable payload names nothing and is ignored.
    /// </summary>
    private async Task<HashSet<Guid>> ReadBlockedScenarioIdsAsync(CancellationToken cancellationToken)
    {
        var rows = await _conditionRepository.GetOpenByKindAsync(AgentTriggerKinds.NextPeriodSchedulingDue, cancellationToken);
        var ids = new HashSet<Guid>();
        foreach (var row in rows)
        {
            if (TryReadBlockedScenarioId(row, out var scenarioId))
            {
                ids.Add(scenarioId);
            }
        }

        return ids;
    }

    private static bool TryReadBlockedScenarioId(AgentCondition row, out Guid scenarioId)
    {
        scenarioId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(row.PayloadJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(row.PayloadJson);
            var root = document.RootElement;
            return root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty(NextPeriodAutoCommitBlockedTriggerEvent.BlockReasonPayloadKey, out _)
                && root.TryGetProperty(NextPeriodAutoCommitBlockedTriggerEvent.ScenarioIdPayloadKey, out var idElement)
                && idElement.ValueKind == JsonValueKind.String
                && idElement.TryGetGuid(out scenarioId);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
