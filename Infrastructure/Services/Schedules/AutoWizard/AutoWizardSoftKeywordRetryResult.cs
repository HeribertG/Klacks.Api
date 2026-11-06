// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules.AutoWizard;

namespace Klacks.Api.Infrastructure.Services.Schedules.AutoWizard;

/// <summary>
/// What became of an AutoWizard run's soft-keyword retry.
/// </summary>
/// <param name="Outcome">One of AutoWizardSoftKeywordRetryOutcomes.</param>
/// <param name="Alternative">The kept soft scenario; null unless Outcome is Kept.</param>
internal sealed record AutoWizardSoftKeywordRetryResult(string Outcome, AutoWizardSoftAlternativeDto? Alternative)
{
    public static AutoWizardSoftKeywordRetryResult Of(string outcome) => new(outcome, null);
}
