// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A scheduling rule whose imported rate revisions actually changed, with the earliest affected date.
/// </summary>
/// <param name="RuleId">Id of the scheduling rule owning the revision</param>
/// <param name="ValidFrom">Earliest ValidFrom touched by the change</param>

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed record RateRevisionChange(Guid RuleId, DateOnly ValidFrom);
