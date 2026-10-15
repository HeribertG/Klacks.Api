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
    /// <summary>When the reaction time started (first notification, else the planned wake instant, else creation);
    /// the intervention list measures the remaining reaction time from here.</summary>
    public DateTime? StartedAtUtc { get; set; }
    /// <summary>Name of the group the chain belongs to, null when it has none or the group is gone.</summary>
    public string? GroupName { get; set; }
    /// <summary>Trigger kind of the finding an approval chain asks about, null for absence coverage.</summary>
    public string? FindingKind { get; set; }
    /// <summary>Group of the chain, for the deep link into the schedule.</summary>
    public Guid? GroupId { get; set; }
    /// <summary>The prepared recovery scenario, only while it is still active (not accepted, rejected or deleted).</summary>
    public Guid? ScenarioId { get; set; }
    /// <summary>For a chain registered ahead of time: when the first planner will be woken; null once it is running.</summary>
    public DateTime? WakeNotBeforeUtc { get; set; }

    /// <summary>True when the requesting user currently holds a Notified stage on this chain.</summary>
    public bool CanAcknowledge { get; set; }

    /// <summary>True when the requesting user may cancel this chain (administrator, supervisor or holder of an open stage of it).</summary>
    public bool CanCancel { get; set; }

    public List<EscalationStageSummaryResource> Stages { get; set; } = [];
}
