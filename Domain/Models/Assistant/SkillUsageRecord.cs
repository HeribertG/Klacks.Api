// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public class SkillUsageRecord : BaseEntity
{
    public required string SkillName { get; set; }
    public required SkillCategory Category { get; set; }
    public required Guid UserId { get; set; }
    public required Guid TenantId { get; set; }
    public LLMProviderType? ProviderId { get; set; }
    public string? ModelId { get; set; }
    public string? SessionId { get; set; }
    public string? ParametersJson { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Join key to the chat turn (llm_usages.id / skill_selection_trajectories.turn_id), set whenever
    /// the execution ran inside a chat turn. Enables "Turn → gewählter Skill → Ausführungsergebnis".
    /// </summary>
    public Guid? TurnId { get; set; }

    /// <summary>
    /// Failure class for rows written by the pre-dispatch failure tracking (W1.2). Null for normal
    /// dispatches — those carry Success/ErrorMessage instead.
    /// </summary>
    public SkillFailureKind? FailureKind { get; set; }

    /// <summary>
    /// Lifecycle state of a UiAction execution (W1.4). Dispatched is written at dispatch time; the
    /// frontend reports Completed/Failed afterwards, so Success stops meaning "the browser surely did
    /// it" and starts meaning "the browser said it did it". Null for non-UiAction rows.
    /// </summary>
    public UiActionStatus? UiActionStatus { get; set; }
}
