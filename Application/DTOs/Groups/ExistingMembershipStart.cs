// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// The start of a group membership a person already holds, handed to the start plan so an entry-date run can report
/// members whose start lies after their employment entry.
/// </summary>
/// <param name="ClientId">The person</param>
/// <param name="ValidFrom">Start of the existing membership; null means open (no lower bound)</param>
/// <param name="ItemId">Id of the membership (GroupItem); set only when the membership belongs to a target group of the run and may be pulled forward, otherwise null (reported, never changed)</param>
/// <param name="GroupId">Group the membership belongs to; set together with ItemId</param>
/// <param name="ValidUntil">End of the existing membership; an ended membership is never pulled forward</param>
public sealed record ExistingMembershipStart(
    Guid ClientId, DateTime? ValidFrom, Guid? ItemId = null, Guid? GroupId = null, DateTime? ValidUntil = null);
