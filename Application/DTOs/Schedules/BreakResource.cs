// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Application.DTOs.Schedules;

public class BreakResource : ScheduleEntryResource
{
    public Guid AbsenceId { get; set; }

    public AbsenceResource? Absence { get; set; }

    public MultiLanguage? Description { get; set; }
}
