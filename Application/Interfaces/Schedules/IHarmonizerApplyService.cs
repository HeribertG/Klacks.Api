// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Materialises a cached harmonizer result into Work entities. Always writes into a new
/// AnalyseScenario so the source schedule remains untouched and the user can compare or
/// roll back. After materialisation the end-state compliance diff of the new scenario versus
/// the real plan is evaluated (redistributions must never be double-counted row by row).
/// </summary>
public interface IHarmonizerApplyService
{
    /// <param name="nameKind">Selects the localized scenario name prefix; null keeps the service default (Harmonized, or Llm for the holistic harmonizer).</param>
    /// <param name="language">The planner's language for the name prefix; null falls back to the installation language.</param>
    /// <param name="captureRun">When true, writes a WizardRunCapture row for the (deferred) preference-learner.
    /// Wizard 4 sets this false because its runner writes its own composite capture after materialising through this path.</param>
    /// <param name="evaluateCompliance">When true, evaluates the end-state compliance diff of the new scenario
    /// versus the real plan and returns it as the report. Wizard 4 sets this false (autonomous background
    /// path with no reader — protection happens at the accept gate).</param>
    Task<(AnalyseScenarioResource Scenario, IReadOnlyList<Guid> CreatedWorkIds, ScenarioComplianceReport? ComplianceReport)> ApplyAsScenarioAsync(
        Guid jobId,
        Guid? groupId,
        CancellationToken ct,
        ScenarioNameKind? nameKind = null,
        string? language = null,
        bool captureRun = true,
        bool evaluateCompliance = true);
}
