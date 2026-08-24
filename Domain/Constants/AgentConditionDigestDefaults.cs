// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Named constants for the daily condition-ledger digest (Etappe 3h), so none of them are magic
/// literals inside AgentConditionDigestService or AgentConditionDigestBackgroundService.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class AgentConditionDigestDefaults
{
    /// <summary>Default local time of day the digest fires, as a TimeSpan-parseable "HH:mm" string. Configurable via BackgroundServiceOptions.AgentConditionDigestTimeOfDayLocal.</summary>
    public const string DefaultTimeOfDayLocal = "06:30";

    /// <summary>How many of a planner's most urgent findings are named individually in the digest's structured payload.</summary>
    public const int TopFindingsCount = 5;

    /// <summary>A finding detected within this many hours of the digest run counts as "new" rather than "still open".</summary>
    public const int NewWithinHours = 24;

    /// <summary>
    /// Upper bound on how many scoped rows AgentConditionRepository.GetOpenForScopeAsync materialises per
    /// planner for the severity breakdown. Verified against the dev database (2026-08-24): an
    /// accumulated test backlog of ~2900 open High-severity rows for one unrestricted scope hit an
    /// earlier, lower cap and made every bucket except High read as zero even though real Medium/Low
    /// rows existed beyond it - AgentConditionDigestService.BuildAndDispatchDigestsAsync therefore
    /// re-queries the true total via CountOpenForScopeAsync whenever this cap is hit, but the per-severity
    /// breakdown stays a read of only the first ScopeQueryCap rows (sorted severity-first), so this value
    /// should stay well above any realistic single-scope backlog rather than be tuned down like a display
    /// page size.
    /// </summary>
    public const int ScopeQueryCap = 1000;
}
