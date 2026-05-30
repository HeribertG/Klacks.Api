// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// An employee that cannot cover a slot, with the reason (absent, collision, rest violation, blacklisted).
/// </summary>
/// <param name="ClientId">Excluded employee</param>
/// <param name="Name">Display name</param>
/// <param name="Reason">Why they were excluded</param>
public sealed record ExcludedCandidate(Guid ClientId, string Name, string Reason);
