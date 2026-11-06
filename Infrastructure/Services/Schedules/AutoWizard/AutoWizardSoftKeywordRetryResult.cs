// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules.AutoWizard;

namespace Klacks.Api.Infrastructure.Services.Schedules.AutoWizard;

/// <summary>
/// What became of an AutoWizard run's soft-keyword retry.
/// </summary>
/// <param name="Outcome">One of AutoWizardSoftKeywordRetryOutcomes.</param>
/// <param name="Alternative">The kept soft scenario; null unless Outcome is Kept.</param>
/// <param name="PromotedChain">The soft chain that replaces the strict result; null unless Outcome is Promoted.</param>
internal sealed record AutoWizardSoftKeywordRetryResult(
    string Outcome,
    AutoWizardSoftAlternativeDto? Alternative,
    AutoWizardChainOutcome? PromotedChain = null)
{
    public static AutoWizardSoftKeywordRetryResult Of(string outcome) => new(outcome, null);

    public static AutoWizardSoftKeywordRetryResult Promoted(AutoWizardChainOutcome chain)
        => new(AutoWizardSoftKeywordRetryOutcomes.Promoted, null, chain);
}
