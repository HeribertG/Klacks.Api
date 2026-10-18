// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// A schedule finding the booking would newly introduce that refuses nothing (a warning such as rest time or overtime).
/// </summary>
/// <param name="Comment">Validation key of the finding</param>
/// <param name="Severity">Severity of the finding</param>
/// <param name="Date">Day the finding concerns</param>
/// <param name="CommentParams">Parameters of the validation key</param>
public sealed record WorkBookingAdvisory(
    string Comment,
    ScheduleValidationType Severity,
    DateOnly Date,
    IReadOnlyDictionary<string, string> CommentParams);
