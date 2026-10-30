// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Clients;

/// <summary>
/// What is booked for the employee in the main plan after the exit date (no scenario rows, container children left out;
/// a container parent counts once).
/// </summary>
/// <param name="WorksAfterExit">Work entries after the exit date</param>
/// <param name="WorksAlreadyPast">Of those, the ones whose day is before the company's today</param>
/// <param name="FirstWorkDate">Day of the first work entry after the exit date</param>
/// <param name="LastWorkDate">Day of the last work entry after the exit date</param>
/// <param name="AbsenceDaysAfterExit">Days with an absence entry after the exit date, on-call duty not included</param>
/// <param name="OnCallDaysAfterExit">Days with an on-call entry after the exit date</param>
/// <param name="ApprovedEntriesAfterExit">Work and absence entries after the exit date on an approved day (changeable by administrators and supervisors only)</param>
/// <param name="ClosedEntriesAfterExit">Work and absence entries after the exit date on a closed period (changeable by nobody until the period is reopened)</param>
public sealed record EmployeeExitPlanFacts(
    int WorksAfterExit,
    int WorksAlreadyPast,
    DateOnly? FirstWorkDate,
    DateOnly? LastWorkDate,
    int AbsenceDaysAfterExit,
    int OnCallDaysAfterExit,
    int ApprovedEntriesAfterExit,
    int ClosedEntriesAfterExit);
