// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared limits and codes of the period issue list: the validation loader truncates its findings at
/// <see cref="MaxValidationIssues"/> (ordered by date, so the latest days are cut) and the issue query marks the
/// schedule notes it adds with <see cref="ScheduleNoteCode"/>. Callers that count findings use both to tell a
/// complete list from a truncated one.
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class PeriodIssueLimits
{
    public const int MaxValidationIssues = 500;

    public const string ScheduleNoteCode = "ScheduleNote";
}
