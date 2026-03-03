// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Application.DTOs.Schedules;

public class BreakResource : ScheduleEntryResource
{
    public Guid AbsenceId { get; set; }

    public MultiLanguage? Description { get; set; }
}
