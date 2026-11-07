// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.DTOs.Schedules;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Period hours a single-Work write answers with after IScheduleCompletionService.CompleteWorkWriteAsync. A value
/// type, so an unconfigured substitute yields both members null instead of a null result.
/// </summary>
/// <param name="PeriodHours">Period hours of the Work's client; null when neither the recalculation nor the live fallback succeeded</param>
/// <param name="PreviousPeriodHours">Period hours of the previous client when the write moved the Work to another client, otherwise null</param>
public readonly record struct WorkWriteCompletion(PeriodHoursResource? PeriodHours, PeriodHoursResource? PreviousPeriodHours);
