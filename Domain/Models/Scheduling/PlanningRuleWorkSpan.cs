// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>One persisted Work row as the planning-rule carry-in needs it (no names, ids only).</summary>
/// <param name="ClientId">Owning client</param>
/// <param name="Date">Calendar date the work is anchored to (Work.CurrentDate)</param>
/// <param name="StartTime">Wall-clock start</param>
/// <param name="EndTime">Wall-clock end; not after start wraps midnight</param>
/// <param name="WorkTime">Paid hours</param>
/// <param name="WorkId">Id of the Work row; lets a pre-commit check take a vacated work out of the plan</param>

namespace Klacks.Api.Domain.Models.Scheduling;

public sealed record PlanningRuleWorkSpan(Guid ClientId, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, decimal WorkTime, Guid WorkId = default);
