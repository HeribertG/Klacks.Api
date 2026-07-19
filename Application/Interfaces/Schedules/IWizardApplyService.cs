// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Schedules.Wizard;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Applies a cached wizard scenario to the DB by materialising its non-locked tokens as Work entities.
/// Every materialisation path runs through the shared compliance partition guardrail first, so
/// Block-mode rule violations are skipped (or let through via an authorised supervisor override)
/// and warnings are surfaced on the outcome.
/// </summary>
public interface IWizardApplyService
{
    /// <summary>
    /// Materialises the scenario identified by <paramref name="jobId"/> into the world the wizard ran
    /// on (real plan, or the source scenario when the run carried an analyse token). Returns the
    /// created Work ids plus the compliance partition report.
    /// Throws <see cref="InvalidOperationException"/> when no scenario is cached.
    /// </summary>
    /// <param name="overrideBlock">Requests the K1 supervisor override for Block-mode violations.</param>
    Task<WizardApplyOutcome> ApplyAsync(Guid jobId, bool overrideBlock, CancellationToken ct);

    /// <summary>
    /// Creates a new AnalyseScenario with a full schedule clone, then writes the wizard's
    /// non-locked tokens into it — gated through the compliance partition against the freshly
    /// flushed clone world under the NEW scenario token. Returns the created scenario resource and
    /// the partition outcome. Throws <see cref="InvalidOperationException"/> when no scenario is
    /// cached for <paramref name="jobId"/>.
    /// </summary>
    /// <param name="overrideBlock">Requests the K1 supervisor override for Block-mode violations.</param>
    /// <param name="namePrefixOverride">Overrides the default "Plan" name prefix; null keeps the default.</param>
    Task<(AnalyseScenarioResource Scenario, WizardApplyOutcome Outcome)> ApplyAsScenarioAsync(
        Guid jobId,
        Guid? groupId,
        bool overrideBlock,
        CancellationToken ct,
        string? namePrefixOverride = null);
}
