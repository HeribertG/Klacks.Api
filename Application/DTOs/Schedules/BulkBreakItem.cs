// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Application.DTOs.Schedules;

public class BulkBreakItem
{
    public Guid ClientId { get; set; }

    public Guid AbsenceId { get; set; }

    public DateOnly CurrentDate { get; set; }

    public decimal WorkTime { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Information { get; set; }

    public MultiLanguage? Description { get; set; }

    public Guid? AnalyseToken { get; set; }
}
