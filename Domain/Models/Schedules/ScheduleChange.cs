// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Schedules;

public class ScheduleChange : BaseEntity
{
    public Guid ClientId { get; set; }
    public DateOnly ChangeDate { get; set; }
}
