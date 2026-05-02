// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Scheduling;

/// <summary>
/// Effective scheduling-policy values for a single client at a given date. Resolved by combining
/// the global default settings with the client's contract / scheduling-rule overrides. Both the
/// wizard placement engine and the post-hoc validator MUST consume the same record so a value
/// accepted by one cannot be flagged by the other.
/// </summary>
/// <param name="MinRestHours">Minimum rest hours between two work blocks</param>
/// <param name="MaxDailyHours">Maximum hours per calendar day</param>
/// <param name="MaxConsecutiveDays">Maximum consecutive work days without a rest day</param>
public sealed record SchedulingPolicy(
    TimeSpan MinRestHours,
    TimeSpan MaxDailyHours,
    int MaxConsecutiveDays);
