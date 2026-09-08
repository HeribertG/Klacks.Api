// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Property names inside AgentCondition.PayloadJson that the welcome focus reads. The ledger
/// stores JsonSerializer.Serialize(triggerEvent.Payload) verbatim, so these are exactly the
/// dictionary keys the period trigger events declare - camelCase, DateOnly rendered as
/// "yyyy-MM-dd" and the day counts as JSON numbers.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class WelcomeFocusPayloadKeys
{
    public const string GroupName = "groupName";
    public const string PeriodEndDate = "periodEndDate";
    public const string PeriodStartDate = "periodStartDate";
    public const string DaysOverdue = "daysOverdue";
    public const string DaysUntilDue = "daysUntilDue";
    public const string DaysUntilStart = "daysUntilStart";
}
