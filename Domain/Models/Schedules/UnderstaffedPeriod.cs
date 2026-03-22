// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Period of understaffing - fewer active employees than required.
/// </summary>
/// <param name="Start">Start of understaffing</param>
/// <param name="End">End of understaffing</param>
/// <param name="StaffCount">Actual staffing in the period</param>
/// <param name="RequiredStaff">Required minimum staffing</param>
namespace Klacks.Api.Domain.Models.Schedules;

public record UnderstaffedPeriod(
    DateTime Start,
    DateTime End,
    int StaffCount,
    int RequiredStaff);
