// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// Hours-balance sweep across a group's active employee members for one period.
/// </summary>
/// <param name="GroupId">UUID of the resolved group.</param>
/// <param name="GroupName">Resolved name of the group.</param>
/// <param name="StartDate">Period start.</param>
/// <param name="EndDate">Period end.</param>
/// <param name="MemberCount">Number of active employee members included in the sweep.</param>
/// <param name="Members">Per-member balances, sorted ascending by Balance (most under-target first).</param>
public record GroupHoursBalanceResult(
    Guid GroupId,
    string GroupName,
    DateOnly StartDate,
    DateOnly EndDate,
    int MemberCount,
    IReadOnlyList<GroupMemberHoursBalance> Members);
