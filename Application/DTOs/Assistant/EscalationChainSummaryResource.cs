// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public class EscalationChainSummaryResource
{
    public Guid Id { get; set; }

    public string Purpose { get; set; } = string.Empty;

    public Guid? WorkId { get; set; }

    public Guid? ConditionId { get; set; }

    public string AbsentClientName { get; set; } = string.Empty;

    public DateTime? ShiftStartUtc { get; set; }

    public DateTime DeadlineUtc { get; set; }

    /// <summary>True when the requesting user currently holds a Notified stage on this chain.</summary>
    public bool CanAcknowledge { get; set; }

    public List<EscalationStageSummaryResource> Stages { get; set; } = [];
}
