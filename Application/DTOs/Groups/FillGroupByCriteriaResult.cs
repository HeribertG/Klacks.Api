// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// Result of filling a group with clients that match a set of criteria.
/// </summary>
/// <param name="Applied">False for a dry-run preview, true when the memberships were persisted.</param>
/// <param name="GroupName">Resolved name of the target group.</param>
/// <param name="TotalMatchCount">Total number of clients matching the criteria (may exceed the returned list).</param>
/// <param name="AddedCount">Number of new memberships created (only meaningful when Applied is true).</param>
/// <param name="AlreadyMemberCount">Number of matched clients that were already members of the group.</param>
/// <param name="Clients">The matched clients that were previewed or added.</param>
public record FillGroupByCriteriaResult(
    bool Applied,
    string GroupName,
    int TotalMatchCount,
    int AddedCount,
    int AlreadyMemberCount,
    IReadOnlyList<ClientSearchItem> Clients);
