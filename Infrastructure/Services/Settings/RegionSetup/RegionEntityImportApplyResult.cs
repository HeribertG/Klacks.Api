// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What an applied entity import changed that a post-commit surcharge recalculation depends on.
/// </summary>
/// <param name="RateRevisionChanges">Rate revisions whose content actually changed</param>
/// <param name="ChangedSurchargeRuleIds">Pre-existing presets whose surcharge fields actually changed</param>

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed record RegionEntityImportApplyResult(
    List<RateRevisionChange> RateRevisionChanges,
    List<Guid> ChangedSurchargeRuleIds)
{
    public static RegionEntityImportApplyResult None => new([], []);
}
