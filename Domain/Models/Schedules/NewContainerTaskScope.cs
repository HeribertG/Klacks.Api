// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// Groups and validity of a container that does not exist yet.
/// </summary>
/// <param name="GroupIds">Groups of the container; empty = no group filter</param>
/// <param name="ValidFrom">First day of validity</param>
/// <param name="ValidUntil">Last day of validity; null = open ended</param>
public sealed record NewContainerTaskScope(IReadOnlyCollection<Guid> GroupIds, DateOnly ValidFrom, DateOnly? ValidUntil);
