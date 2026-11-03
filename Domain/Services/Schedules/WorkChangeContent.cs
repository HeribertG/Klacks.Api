// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// Whether a work change as requested is the stored one in what the write checks look at: Work, type, person and times.
/// A row that is the same is not checked again.
/// </summary>
public static class WorkChangeContent
{
    /// <param name="stored">The work change as stored</param>
    /// <param name="requested">The work change rebuilt from a request</param>
    public static bool AreSame(WorkChange stored, WorkChange requested)
        => stored.WorkId == requested.WorkId
            && stored.Type == requested.Type
            && stored.ReplaceClientId == requested.ReplaceClientId
            && stored.StartTime == requested.StartTime
            && stored.EndTime == requested.EndTime
            && stored.ChangeTime == requested.ChangeTime;
}
