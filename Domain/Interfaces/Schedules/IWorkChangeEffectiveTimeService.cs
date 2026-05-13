// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

/// <summary>
/// Computes the effective clock-time window for a WorkChange entry, mirroring
/// the offset logic of the get_schedule_entries stored procedure.
/// </summary>
public interface IWorkChangeEffectiveTimeService
{
    Task<(TimeOnly Start, TimeOnly End)> GetEffectiveTimesAsync(
        WorkChange workChange, Work work, Shift? shift);
}
