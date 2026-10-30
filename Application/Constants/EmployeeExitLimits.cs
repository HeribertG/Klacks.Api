// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Limit of the employee exit advice: entries are looked for in the <see cref="HorizonDays"/> days after the exit
/// date; anything booked further out is not counted, and the advice states the last day it looked at.
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class EmployeeExitLimits
{
    public const int HorizonDays = 366;
}
