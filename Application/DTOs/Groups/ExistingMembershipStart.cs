// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// The start of a group membership a person already holds, handed to the start plan so an entry-date run can report
/// members whose start lies after their employment entry.
/// </summary>
/// <param name="ClientId">The person</param>
/// <param name="ValidFrom">Start of the existing membership; null means open (no lower bound)</param>
public sealed record ExistingMembershipStart(Guid ClientId, DateTime? ValidFrom);
