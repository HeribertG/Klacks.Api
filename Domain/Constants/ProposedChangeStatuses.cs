// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

public static class ProposedChangeStatuses
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";

    /// <summary>
    /// Applied by the loop itself because the routing regression gate stayed green. Written from stage G2
    /// on; the admin card lists it so an automatic change is never invisible.
    /// </summary>
    public const string AppliedAuto = "applied_auto";

    /// <summary>
    /// Withheld because applying it would have turned a previously green golden case red.
    /// </summary>
    public const string BlockedRegression = "blocked_regression";

    /// <summary>
    /// The statuses the "Klacksy learned" card shows as editable description rows: still open, applied
    /// automatically, or blocked. Approved and rejected rows are history and stay out.
    /// </summary>
    public static readonly IReadOnlyList<string> ReviewableForLearning = [Pending, AppliedAuto, BlockedRegression];
}

public static class ProposedChangeFields
{
    public const string Description = "description";
}
