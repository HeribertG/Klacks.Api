// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Schedules;

public readonly record struct WeekdayPattern(bool Mon, bool Tue, bool Wed, bool Thu, bool Fri, bool Sat, bool Sun)
{
    public int FlaggedDays =>
        (Mon ? 1 : 0) + (Tue ? 1 : 0) + (Wed ? 1 : 0) +
        (Thu ? 1 : 0) + (Fri ? 1 : 0) + (Sat ? 1 : 0) + (Sun ? 1 : 0);

    public static WeekdayPattern FromContract(Contract contract) => new(
        contract.WorkOnMonday,
        contract.WorkOnTuesday,
        contract.WorkOnWednesday,
        contract.WorkOnThursday,
        contract.WorkOnFriday,
        contract.WorkOnSaturday,
        contract.WorkOnSunday);

    public bool IsActiveOn(DateOnly date) => date.DayOfWeek switch
    {
        DayOfWeek.Monday => Mon,
        DayOfWeek.Tuesday => Tue,
        DayOfWeek.Wednesday => Wed,
        DayOfWeek.Thursday => Thu,
        DayOfWeek.Friday => Fri,
        DayOfWeek.Saturday => Sat,
        DayOfWeek.Sunday => Sun,
        _ => false,
    };
}
