// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistent row of an AssistantLastAction. Deliberately not a BaseEntity: the record is ephemeral,
/// is overwritten on every tool-calling turn and is hard-deleted on expiry, exactly like
/// PendingRecipeRow. Every text column is capped in code as well as in the configuration, because EF
/// InMemory ignores HasMaxLength and a store test would otherwise pass without the cap applying.
/// </summary>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed class AssistantLastActionRow
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string ConversationId { get; set; } = string.Empty;

    public string UserMessage { get; set; } = string.Empty;

    public string CallsJson { get; set; } = Constants.GracefulCorrectionDefaults.EmptyJsonArray;

    public string AssistantAnswerExcerpt { get; set; } = string.Empty;

    public string ClarificationSkillsJson { get; set; } = Constants.GracefulCorrectionDefaults.EmptyJsonArray;

    public DateTime CreateTimeUtc { get; set; }

    public DateTime? SupersededAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }
}
