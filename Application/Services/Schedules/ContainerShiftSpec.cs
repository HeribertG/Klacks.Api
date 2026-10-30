// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What a skill decides about a container it creates; ContainerShiftFactory turns it into the sealed container order.
/// </summary>
/// <param name="Name">Name of the container</param>
/// <param name="Abbreviation">Abbreviation of the container</param>
/// <param name="FromTime">Start of the container's time frame</param>
/// <param name="UntilTime">End of the container's time frame</param>
/// <param name="FromDate">First day of validity</param>
/// <param name="UntilDate">Last day of validity; null = open-ended</param>
/// <param name="WorkTime">Work time of the time frame in hours</param>
/// <param name="MacroId">Default Shift-category macro of the container; null when none exists</param>
/// <param name="IsoWeekdays">ISO weekdays (1=Monday..7=Sunday) the container is active on</param>
/// <param name="GroupItems">Group links the container is created with</param>
/// <param name="UserName">Name of the user recorded as creator</param>

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Schedules;

public sealed record ContainerShiftSpec(
    string Name,
    string Abbreviation,
    TimeOnly FromTime,
    TimeOnly UntilTime,
    DateOnly FromDate,
    DateOnly? UntilDate,
    decimal WorkTime,
    Guid? MacroId,
    IReadOnlySet<int> IsoWeekdays,
    List<GroupItem> GroupItems,
    string UserName);
