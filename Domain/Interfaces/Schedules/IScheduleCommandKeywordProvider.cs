// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

/// <summary>
/// Resolves the currently effective planning-command keyword tokens from Settings.
/// </summary>
public interface IScheduleCommandKeywordProvider
{
    Task<ScheduleCommandKeywordSet> GetAsync(CancellationToken cancellationToken = default);
}
