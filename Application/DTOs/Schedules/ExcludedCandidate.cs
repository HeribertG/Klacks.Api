// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// An employee that cannot cover a slot, with the reason (absent, collision, rest violation, blacklisted).
/// </summary>
/// <param name="ClientId">Excluded employee</param>
/// <param name="Name">Display name</param>
/// <param name="Reason">Why they were excluded</param>
/// <param name="IsBorrowed">True when the employee is no member of the requested group but of another group under the same root</param>
public sealed record ExcludedCandidate(Guid ClientId, string Name, string Reason, bool IsBorrowed = false);
