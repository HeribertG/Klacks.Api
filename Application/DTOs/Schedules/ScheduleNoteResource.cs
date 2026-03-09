// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

public class ScheduleNoteResource
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    public DateOnly CurrentDate { get; set; }

    public string Content { get; set; } = string.Empty;
}
