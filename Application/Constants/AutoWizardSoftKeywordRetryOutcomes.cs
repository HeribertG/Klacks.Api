// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What became of the soft-keyword retry of an AutoWizard run (AutoWizardJobResultDto.SoftKeywordRetryOutcome).
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class AutoWizardSoftKeywordRetryOutcomes
{
    /// <summary>The start did not ask for a retry, or the run itself was planned in Soft mode.</summary>
    public const string NotRequested = "NotRequested";

    /// <summary>The strict scenario has no open slot, or no employee has a restricting schedule command in the period.</summary>
    public const string NotNeeded = "NotNeeded";

    /// <summary>The soft scenario fills more slots; it is offered as SoftAlternative.</summary>
    public const string Kept = "Kept";

    /// <summary>The soft scenario fills no more slots than the strict one and was discarded.</summary>
    public const string NotBetter = "NotBetter";

    /// <summary>The soft chain failed, timed out or was cancelled; the strict scenario is the only result.</summary>
    public const string Failed = "Failed";
}
