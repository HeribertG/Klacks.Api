// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Email;

public class EmailAnalysis : BaseEntity
{
    public Guid ReceivedEmailId { get; set; }

    public virtual ReceivedEmail? ReceivedEmail { get; set; }

    public Guid? ClientId { get; set; }

    public EntityTypeEnum? ClientType { get; set; }

    public EmailIntent Intent { get; set; }

    public string Summary { get; set; } = string.Empty;

    public DateOnly? FromDate { get; set; }

    public DateOnly? UntilDate { get; set; }

    public int? StartHour { get; set; }

    public int? EndHour { get; set; }

    public string? Weekdays { get; set; }

    public string? ScheduleCommands { get; set; }

    public DateTime AnalyzedAt { get; set; }

    public string? FailureReason { get; set; }
}
