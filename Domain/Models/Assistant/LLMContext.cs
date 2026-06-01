// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Models.Assistant;

public class LLMContext
{
    public string Message { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public List<string> UserRights { get; set; } = new();

    public List<LLMFunction> AvailableFunctions { get; set; } = new();

    public string? ConversationId { get; set; }

    public string? ModelId { get; set; }

    public string? Language { get; set; }

    public AssistantPageContext? PageContext { get; set; }

    /// <summary>
    /// Effective scheduling policy of the client currently in scope (PageContext.SelectedClientId),
    /// resolved in the Application layer when the turn is a scheduling task. Null when no client is in
    /// scope or the turn is not a scheduling task. Rendered as a tightly-scoped per-client rule block.
    /// </summary>
    public SchedulingPolicy? ScopedClientPolicy { get; set; }
}
