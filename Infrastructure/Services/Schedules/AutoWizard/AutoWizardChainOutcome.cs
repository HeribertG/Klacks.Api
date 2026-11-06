// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules.Wizard;
using Klacks.Api.Application.Services.Schedules.AutoWizard;

namespace Klacks.Api.Infrastructure.Services.Schedules.AutoWizard;

/// <summary>
/// Outcome of one finished AutoWizard stage chain.
/// </summary>
/// <param name="FinalScenario">The last scenario the chain produced (Holistic Harmonizer, or Harmonizer when stage 3 was skipped).</param>
/// <param name="WizardOutcome">Compliance outcome of the Wizard 1 stage's materialisation.</param>
/// <param name="HarmonizationSkippedReason">Why stage 3 was skipped; null when it ran.</param>
internal sealed record AutoWizardChainOutcome(
    AutoWizardStageScenario FinalScenario,
    WizardApplyOutcome WizardOutcome,
    string? HarmonizationSkippedReason);
