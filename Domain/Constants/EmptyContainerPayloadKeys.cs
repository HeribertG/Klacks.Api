// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The key names EmptyContainerTriggerEvent writes into AgentCondition.PayloadJson and
/// EmptyContainerRemediationBinder reads back out of it. Shared constants rather than literals at both
/// ends because the payload is PERSISTED: a row opened today is bound weeks later, so a spelling that
/// drifts on one side does not fail loudly, it silently produces an unbindable condition.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class EmptyContainerPayloadKeys
{
    public const string ShiftId = "shiftId";
    public const string ContainerName = "containerName";
    public const string FromDate = "fromDate";
    public const string UntilDate = "untilDate";
    public const string GroupIds = "groupIds";

    /// <summary>Added in Etappe 5b; rows opened before it are unbindable and are never claimed.</summary>
    public const string StartShift = "startShift";

    public const string EndShift = "endShift";
    public const string IsoWeekdays = "isoWeekdays";
    public const string IsHoliday = "isHoliday";
    public const string IsWeekdayAndHoliday = "isWeekdayAndHoliday";
}
