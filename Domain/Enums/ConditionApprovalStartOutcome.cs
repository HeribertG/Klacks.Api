// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// What asking for an approval chain on one condition led to. Every value other than Started is a
/// fail-closed outcome the tick counts and logs but never retries within the same run: the chain that
/// is already Running is the one whose answer counts, a chain that ended today must not be restarted
/// before the next company day, a roster without an eligible approver has nobody to ask, and a start
/// the chain service declined (a concurrent instance won the unique index) is somebody else's chain.
/// </summary>
public enum ConditionApprovalStartOutcome
{
    Started = 0,
    ChainAlreadyRunning = 1,
    WaitingForNextCompanyDay = 2,
    NoEligibleApprover = 3,
    NotStarted = 4
}
