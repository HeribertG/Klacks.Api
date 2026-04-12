// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Schedule command entry for a specific client and date (e.g. FREE, EARLY, LATE, NIGHT).
/// </summary>
/// <param name="ClientId">The employee this command applies to</param>
/// <param name="CurrentDate">The date this command applies to</param>
/// <param name="CommandKeyword">The command keyword (e.g. FREE, -EARLY)</param>
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Models.Schedules;

public class ScheduleCommand : BaseEntity
{
    public Guid ClientId { get; set; }
    public Client? Client { get; set; }
    public DateOnly CurrentDate { get; set; }
    public string CommandKeyword { get; set; } = string.Empty;
    public Guid? AnalyseToken { get; set; }
}
