// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// The number of active (non-scenario, non-shift) client memberships of a single group.
/// </summary>
/// <param name="GroupId">Id of the group.</param>
/// <param name="GroupName">Name of the group.</param>
/// <param name="ActiveMemberCount">Number of clients currently and actually assigned to this group.</param>
public record GroupMemberCount(Guid GroupId, string GroupName, int ActiveMemberCount);
