// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Models.Schedules;

public class ScheduleNote : BaseEntity
{
    public Guid ClientId { get; set; }

    public Client? Client { get; set; }

    public DateOnly CurrentDate { get; set; }

    public string Content { get; set; } = string.Empty;

    public Guid? AnalyseToken { get; set; }
}
