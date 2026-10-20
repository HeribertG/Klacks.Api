// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Grouping;

/// <summary>
/// One existing membership that an entry-date run may pull forward: it belongs to a target group, its person has an
/// employment record, the person was employed at the current start, and the current start lies after the employment entry.
/// </summary>
/// <param name="ItemId">The membership (GroupItem)</param>
/// <param name="GroupId">The group of the membership</param>
/// <param name="ClientId">The person</param>
/// <param name="NewStart">The person's employment entry (UTC midnight), the start the membership is pulled forward to</param>
public sealed record PullForwardCandidate(Guid ItemId, Guid GroupId, Guid ClientId, DateTime NewStart);
