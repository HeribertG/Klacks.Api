// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// Result of the per-turn ambient memory retrieval: the rendered prompt block plus the ids of every
/// memory it injected (pinned, hybrid-matched and 1-hop expansion), so a same-turn get_ai_memories call
/// can dedup its own results against what the model already has in context.
/// </summary>
/// <param name="PromptText">Rendered PERSISTENT KNOWLEDGE block, empty when nothing was injected</param>
/// <param name="InjectedMemoryIds">Ids of every memory already surfaced to the model this turn</param>
public record MemoryRetrievalResult(string PromptText, IReadOnlyList<Guid> InjectedMemoryIds);
