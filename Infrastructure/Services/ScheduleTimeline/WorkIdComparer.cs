// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Services.ScheduleTimeline;

/// <summary>
/// Treats two works as equal when they share an id, so a work loaded both as the client's own and as one the
/// client replaces on is checked once.
/// </summary>
public sealed class WorkIdComparer : IEqualityComparer<Work>
{
    public bool Equals(Work? x, Work? y) => x?.Id == y?.Id;

    public int GetHashCode(Work obj) => obj.Id.GetHashCode();
}
