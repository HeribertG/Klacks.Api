// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Inbound;

public class InboundAnalysis : BaseEntity
{
    public InboundSourceKind SourceKind { get; set; }

    public Guid SourceId { get; set; }

    public string Channel { get; set; } = string.Empty;

    public Guid? ClientId { get; set; }

    public EntityTypeEnum? ClientType { get; set; }

    public EmailIntent Intent { get; set; }

    public EmailConfidence Confidence { get; set; }

    public string Summary { get; set; } = string.Empty;

    public DateOnly? FromDate { get; set; }

    public DateOnly? UntilDate { get; set; }

    public int? StartHour { get; set; }

    public int? EndHour { get; set; }

    public string? Weekdays { get; set; }

    public string? ScheduleCommands { get; set; }

    public DateTime AnalyzedAt { get; set; }

    public string? FailureReason { get; set; }

    public bool NeedsClarification { get; set; }

    public string? ClarificationQuestion { get; set; }
}
