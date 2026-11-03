// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.DTOs.Schedules;

public class BulkBreaksResponse : BulkScheduleEntryResponse
{
    /// <summary>The works the absences took away because nobody covers them.</summary>
    public List<RemovableWork> TakenWorks { get; set; } = [];
}
