// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The dates that follow from a period start and the three delivery inputs.
/// </summary>
/// <param name="PeriodStart">First day of the period the plan is for</param>
/// <param name="SendByDate">Latest day the plan may be sent so it arrives when the announcement period demands</param>
/// <param name="PlanningDoneBy">Latest day planning must be finished, one review buffer before the send date</param>
/// <param name="DaysRemaining">Days from the company's today to PlanningDoneBy; negative when already overdue</param>

namespace Klacks.Api.Domain.Services.Assistant;

public sealed record PlanningDeadline(
    DateOnly PeriodStart,
    DateOnly SendByDate,
    DateOnly PlanningDoneBy,
    int DaysRemaining);
