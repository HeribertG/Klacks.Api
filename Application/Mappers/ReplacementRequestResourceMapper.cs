// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Maps a replacement request row to its API resource and judges short notice with the given threshold.
/// </summary>
/// <param name="row">Stored row</param>
/// <param name="shortNoticeHours">REPLACEMENT_SHORT_NOTICE_HOURS</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Mappers;

public static class ReplacementRequestResourceMapper
{
    public static ReplacementRequestResource ToResource(ReplacementRequest row, int shortNoticeHours)
        => new(
            row.Id,
            row.AbsentClientId,
            row.CandidateClientId,
            row.ShiftId,
            row.Date,
            row.StartTime,
            row.EndTime,
            row.GroupId,
            row.AbsenceId,
            row.Source,
            row.Outcome,
            row.ReportedAtUtc,
            row.ShiftStartUtc,
            row.OutcomeAtUtc,
            row.OutcomeByUserId,
            row.AnalyseToken,
            row.WorkChangeId,
            row.AppliedAtUtc,
            ReplacementShortNoticePolicy.IsShortNotice(row.ReportedAtUtc, row.ShiftStartUtc, shortNoticeHours));
}
