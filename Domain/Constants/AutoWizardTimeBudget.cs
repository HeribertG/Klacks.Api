// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Time budget of the AutoWizard orchestration. The strict chain (Wizard 1, Holistic Harmonizer) keeps its
/// own budget; the optional soft-keyword retry runs the chain a second time under a budget of its own, so the retry can
/// never eat into the strict result. Watchers of a whole run (the next-period auto-commit) derive their wait from
/// <see cref="TotalMinutes"/>.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class AutoWizardTimeBudget
{
    /// <summary>Minutes each single stage may run before the chain gives up on it.</summary>
    public const int StageMinutes = 5;

    /// <summary>Minutes the strict chain (both stages) may take; kept above twice StageMinutes as a reserve.</summary>
    public const int StrictChainMinutes = 15;

    /// <summary>Minutes the soft-keyword retry chain may take; on timeout the strict result is kept alone.</summary>
    public const int SoftKeywordRetryMinutes = 15;

    /// <summary>Minutes a whole run may take, strict chain plus soft-keyword retry.</summary>
    public const int TotalMinutes = StrictChainMinutes + SoftKeywordRetryMinutes;
}
