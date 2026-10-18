// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Machine codes and detail field names of a refused booking that is answered with 400 (sealed day, planning
/// period, work lock level). The code travels as "errorCode" and "code" next to the unchanged English detail, so a
/// client can explain the refusal in the user's language. The 409 refusals live in WorkWriteConflictCodes; together
/// they form the one catalogue of booking refusal codes. A refusal for visibility reasons (hidden client or shift)
/// deliberately has no code: it must stay indistinguishable from "not found".
/// </summary>
public static class WorkBookingRefusalCodes
{
    /// <summary>The day is sealed for the employee (day approval or period close).</summary>
    public const string DaySealed = "DAY_SEALED";

    /// <summary>The day lies outside the employee's membership period.</summary>
    public const string OutsideMembershipPeriod = "OUTSIDE_MEMBERSHIP_PERIOD";

    /// <summary>The day lies outside the employee's group period in the visible groups.</summary>
    public const string OutsideGroupPeriod = "OUTSIDE_GROUP_PERIOD";

    /// <summary>The work itself is locked by a closed period; nobody may change it before the period is reopened.</summary>
    public const string WorkInClosedPeriod = "WORK_IN_CLOSED_PERIOD";

    /// <summary>The work itself is sealed and the caller lacks the role needed to unseal it.</summary>
    public const string WorkSealedRoleRequired = "WORK_SEALED_ROLE_REQUIRED";

    public const string DateField = "date";

    public const string ClientIdField = "clientId";

    public const string FromField = "from";

    public const string UntilField = "until";

    public const string LockLevelField = "lockLevel";

    public const string SealLevelField = "sealLevel";
}
