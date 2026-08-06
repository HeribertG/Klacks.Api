// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public record SkillExecutionContext
{
    public required Guid UserId { get; init; }
    public required Guid TenantId { get; init; }
    public required string UserName { get; init; }
    public required IReadOnlyList<string> UserPermissions { get; init; }
    public string? CurrentPage { get; init; }
    public IReadOnlyList<Guid>? SelectedEntityIds { get; init; }
    public string? UserTimezone { get; init; }
    public LLMProviderType? ProviderId { get; init; }
    public string? ModelId { get; init; }
    public string? SessionId { get; init; }
    public bool BypassAutonomyGate { get; init; }
    public bool SupportsUiActions { get; init; }

    /// <summary>
    /// The caller's bearer token, re-presented when a skill mutates state through the own REST API so
    /// that [Authorize], validation and the request log apply to it like to any other client. Null on
    /// paths that have no caller token; mutating skills must then fail closed rather than write
    /// directly.
    /// </summary>
    public BearerToken? AccessToken { get; init; }

    /// <summary>
    /// Owner the <see cref="AccessToken"/> was minted for on a background path, so long-running work can
    /// mint a fresh one between steps. An internal token deliberately lives only minutes; a plan whose
    /// steps involve LLM calls or a wizard run can outlast it, and the later steps would then fail with
    /// an authentication error rather than a domain one. Null for interactive callers — their token is
    /// the user's own and must never be silently replaced.
    /// </summary>
    public Guid? TokenRenewalOwnerId { get; init; }

    /// <summary>
    /// Ids of memories already injected into the system prompt this turn (ambient retrieval). Null
    /// when unknown/not a chat turn. Consumed by GetAiMemoriesSkill to avoid duplicating full-text
    /// memory content the model already has in context.
    /// </summary>
    public IReadOnlyList<Guid>? InjectedMemoryIds { get; init; }
}
