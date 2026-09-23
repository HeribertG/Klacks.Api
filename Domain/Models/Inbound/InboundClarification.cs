// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Inbound;

public class InboundClarification : BaseEntity
{
    public Guid ClientId { get; set; }

    public InboundSourceKind SourceKind { get; set; }

    public string Channel { get; set; } = string.Empty;

    public string Recipient { get; set; } = string.Empty;

    public Guid OriginalAnalysisId { get; set; }

    public Guid OriginalSourceId { get; set; }

    public string SenderDisplay { get; set; } = string.Empty;

    public string OriginalText { get; set; } = string.Empty;

    public DateTime OriginalReceivedAt { get; set; }

    public string Question { get; set; } = string.Empty;

    public string? ShiftContext { get; set; }

    public DateTime AskedAt { get; set; }

    public DateTime DeadlineAt { get; set; }

    public InboundClarificationStatus Status { get; set; }

    public Guid? AnswerSourceId { get; set; }

    public Guid? ResultAnalysisId { get; set; }

    public string? EmailMessageId { get; set; }

    public DateTime? ResolvedAt { get; set; }
}
