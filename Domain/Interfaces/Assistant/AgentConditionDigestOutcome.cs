// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>Why RunIfDueAsync did, or did not, send anything.</summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public enum AgentConditionDigestOutcome
{
    /// <summary>The installation's local time has not yet reached the configured time of day today.</summary>
    NotDueYet,

    /// <summary>Today's digest was already sent (by this instance or another one) before this call.</summary>
    AlreadyRanToday,

    /// <summary>Another instance won the compare-and-swap claim for today between this call's checks and its attempt to claim it.</summary>
    LostRace,

    /// <summary>This call claimed today and ran the aggregation, whether or not any planner had a finding to report.</summary>
    Ran
}
