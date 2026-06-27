// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// Result of adding the user's selected clients to a group.
/// </summary>
/// <param name="Applied">False for a dry-run preview, true when the memberships were persisted.</param>
/// <param name="GroupName">Resolved name of the target group.</param>
/// <param name="RequestedCount">Number of ids the user selected in the list.</param>
/// <param name="FoundCount">Number of selected ids that resolve to existing clients.</param>
/// <param name="NotFoundCount">Number of selected ids that do not resolve to a client (stale/invalid).</param>
/// <param name="EligibleCount">Number of found clients that are not yet members (would be / were added).</param>
/// <param name="AddedCount">Number of new memberships created (only meaningful when Applied is true).</param>
/// <param name="VerifiedCount">Number of created memberships re-read and confirmed in the database after the write.</param>
/// <param name="AlreadyMemberCount">Number of selected clients that were already members of the group.</param>
/// <param name="Clients">The eligible clients that were previewed or added.</param>
public record AddSelectedClientsToGroupResult(
    bool Applied,
    string GroupName,
    int RequestedCount,
    int FoundCount,
    int NotFoundCount,
    int EligibleCount,
    int AddedCount,
    int VerifiedCount,
    int AlreadyMemberCount,
    IReadOnlyList<ClientSearchItem> Clients);
