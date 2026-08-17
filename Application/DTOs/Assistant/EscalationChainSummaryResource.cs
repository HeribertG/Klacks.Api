// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public class EscalationChainSummaryResource
{
    public Guid Id { get; set; }

    public Guid WorkId { get; set; }

    public string AbsentClientName { get; set; } = string.Empty;

    public DateTime ShiftStartUtc { get; set; }

    public DateTime DeadlineUtc { get; set; }

    /// <summary>True when the requesting user currently holds a Notified stage on this chain.</summary>
    public bool CanAcknowledge { get; set; }

    public List<EscalationStageSummaryResource> Stages { get; set; } = [];
}
