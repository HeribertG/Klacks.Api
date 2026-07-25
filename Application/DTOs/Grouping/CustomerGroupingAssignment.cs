// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// One planned move of a client into a target group.
/// </summary>
/// <param name="ClientId">The client that would move.</param>
/// <param name="ClientName">Display name of the client, for the preview.</param>
/// <param name="CurrentGroupNames">Names of all groups the client currently belongs to.</param>
/// <param name="TargetGroupId">The group the client would be placed in.</param>
/// <param name="TargetGroupName">Name of the target group.</param>
/// <param name="DistanceKm">Air-line distance to the target group, or <c>null</c> when the client was matched by city name instead of by coordinates.</param>
/// <param name="RetireGroupIds">Location memberships that are replaced by the target membership.</param>
/// <param name="RetireGroupNames">Names of the retired groups, in the same order as RetireGroupIds, so a preview can name the memberships that would end.</param>
/// <param name="MatchReason">How the target was found and which address it came from, e.g. "city name (main address)".</param>
public record CustomerGroupingAssignment(
    Guid ClientId,
    string ClientName,
    IReadOnlyList<string> CurrentGroupNames,
    Guid TargetGroupId,
    string TargetGroupName,
    double? DistanceKm,
    IReadOnlyList<Guid> RetireGroupIds,
    IReadOnlyList<string> RetireGroupNames,
    string MatchReason);
