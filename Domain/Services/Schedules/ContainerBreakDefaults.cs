// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// A sub-break of a container belongs to the container's employee, and takes the container's day when it names none.
/// The one definition for the container save and for the checks that judge the sub-breaks before it.
/// </summary>
public static class ContainerBreakDefaults
{
    /// <summary>Sets the employee and, when empty, the day of the sub-break from the container.</summary>
    /// <param name="subBreak">Sub-break rebuilt from a request</param>
    /// <param name="container">The container work the sub-break belongs to</param>
    public static void Apply(Break subBreak, Work container)
    {
        subBreak.ClientId = container.ClientId;
        if (subBreak.CurrentDate == default)
        {
            subBreak.CurrentDate = container.CurrentDate;
        }
    }
}
