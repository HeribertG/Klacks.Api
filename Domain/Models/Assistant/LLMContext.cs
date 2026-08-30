// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Models.Assistant;

public class LLMContext
{
    public string Message { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public List<string> UserRights { get; set; } = new();

    /// <summary>
    /// The caller's bearer token, carried so a mutating skill can re-present it on a self-call
    /// against the own REST API. Null on paths without a caller token.
    /// </summary>
    public BearerToken? AccessToken { get; set; }

    public List<LLMFunction> AvailableFunctions { get; set; } = new();

    public string? ConversationId { get; set; }

    /// <summary>
    /// Unique id of this chat turn, generated once at turn start and carried on the context so every
    /// consumer (trajectory capture, skill usage tracking, LLM usage row) writes the same key.
    /// This is the join key "Turn → gewählter Skill → Ausführungsergebnis" (W1.1).
    /// </summary>
    public Guid? TurnId { get; set; }

    public string? ModelId { get; set; }

    public string? Language { get; set; }

    public AssistantPageContext? PageContext { get; set; }

    /// <summary>
    /// Effective scheduling policy of the client currently in scope (PageContext.SelectedClientId),
    /// resolved in the Application layer when the turn is a scheduling task. Null when no client is in
    /// scope or the turn is not a scheduling task. Rendered as a tightly-scoped per-client rule block.
    /// </summary>
    public SchedulingPolicy? ScopedClientPolicy { get; set; }

    /// <summary>
    /// True when skill retrieval matched at least one non-always-on (domain) skill for this turn,
    /// i.e. the turn is a domain task rather than pure conversation. Used to omit the always-on world-model
    /// ontology block on purely conversational turns (token saving). Null means "unknown" → include the block
    /// (safe default; both chat paths compute this signal via ISkillToolsetAssembler).
    /// </summary>
    public bool? HasDomainSkillContext { get; set; }

    /// <summary>
    /// True when the message was sent from the hands-free voice conversation mode. Suppresses
    /// text-only affordances in the prompt (the SUGGESTION_FORMAT rule) since the user cannot
    /// interact with suggestion chips while speaking, and their generation delays the turn's end.
    /// </summary>
    public bool IsVoiceMode { get; set; }

    /// <summary>
    /// Ready-to-render system prompt block listing entities whose names were deterministically
    /// matched in the user message (canonical spelling plus visible id number), so the model
    /// copies the exact spelling into tool arguments instead of guessing. Null when no
    /// candidates were found; assembled in the Application layer (IEntityCandidateGrounder).
    /// </summary>
    public string? EntityGroundingBlock { get; set; }

    /// <summary>
    /// Ids of every agent memory already surfaced to the model as ambient context this turn (pinned,
    /// hybrid-matched and 1-hop expansion). Set once in PrepareContextAsync and read by
    /// LLMFunctionExecutor when building the SkillExecutionContext, so a same-turn get_ai_memories call
    /// can skip re-surfacing what the model already has. Null when memory retrieval did not run for
    /// this turn (e.g. no default agent, or the message was too short for semantic enrichment).
    /// </summary>
    public IReadOnlyList<Guid>? InjectedMemoryIds { get; set; }

    /// <summary>
    /// True for one-shot background calls (e.g. email intent extraction, spam classification) that are
    /// not a turn in a user-facing Klacksy conversation. Suppresses the entire Klacksy persona/tool-call
    /// system prompt, which otherwise pulls the model into an agentic role — announcing retries, apologizing
    /// for "confusion", or offering to call a tool — even though Message already carries complete,
    /// self-contained instructions and no tools are offered.
    /// </summary>
    public bool IsNonConversational { get; set; }

    /// <summary>
    /// Milliseconds the pre-LLM toolset assembly (skill loading, permission filter, retrieval and
    /// guarantee logic — retrieval being the dominant part) took for this turn. Set unconditionally
    /// by both chat entry points from SkillToolsetResult.AssemblyMs and persisted with the turn's
    /// usage row; null only on paths that never assign it (e.g. background one-shot calls).
    /// </summary>
    public long? ToolsetAssemblyMs { get; set; }

    /// <summary>
    /// Name of the recipe forcing this turn's skill selection, null when none was resolved. Carried on
    /// the context rather than handed through the background-task signature because the context is the
    /// one object both chat entry points already share with the post-turn hooks: on the streaming path
    /// the plan is a local of the same method, on the non-streaming path it lives inside the multi-turn
    /// loop and would otherwise need a widened return tuple to escape.
    /// </summary>
    public string? ActiveRecipeName { get; set; }
}
