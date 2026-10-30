// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Clients;

/// <summary>
/// The facts about one exit that the employee exit advisor needs, gathered by the caller and free of any contact data.
/// </summary>
/// <param name="EmployeeName">Display name of the employee; used in the sentences only</param>
/// <param name="ExitDate">The last day of employment that was asked about</param>
/// <param name="HorizonEnd">Last day after the exit date that was searched for bookings</param>
/// <param name="Membership">The active memberships</param>
/// <param name="Plan">What is booked after the exit date</param>
/// <param name="Contracts">The contract assignments around the exit date</param>
/// <param name="GroupCount">Number of groups in which the employee has a live main-plan group entry (validity not checked)</param>
public sealed record EmployeeExitFacts(
    string EmployeeName,
    DateOnly ExitDate,
    DateOnly HorizonEnd,
    EmployeeExitMembershipFacts Membership,
    EmployeeExitPlanFacts Plan,
    EmployeeExitContractFacts Contracts,
    int GroupCount);
