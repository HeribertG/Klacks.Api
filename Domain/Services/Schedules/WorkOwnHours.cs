// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Which works count their own hours and surcharges in the sums of a person: every work outside a container, and a
/// container child that belongs to someone else than its container (a customer child). A child of the container's own
/// person lies inside the container's time, which the container already counts, so it adds nothing of its own.
/// </summary>

using System.Linq.Expressions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class WorkOwnHours
{
    public static readonly Expression<Func<Work, bool>> Counts =
        w => w.ParentWorkId == null || w.ParentWork!.ClientId != w.ClientId;
}
