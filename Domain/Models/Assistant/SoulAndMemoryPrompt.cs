// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// Result of assembling the per-turn soul and memory prompt, split so the caller can route each
/// part to a different prompt-caching segment. StablePrompt (identity, world-model ontology, rule
/// pack) is byte-identical across turns for the same agent/skill-scope and is safe to cache.
/// VolatilePrompt (pending notes, recently touched entities, mood hint, retrieved memories) changes
/// every turn and must never sit ahead of a cache breakpoint.
/// </summary>
/// <param name="StablePrompt">Cacheable identity, world-model ontology and rule-pack text</param>
/// <param name="VolatilePrompt">Per-turn pending notes, recent entities, mood and memory text</param>
/// <param name="InjectedMemoryIds">Ids of memories injected into VolatilePrompt this turn; null when memory retrieval did not run</param>
public record SoulAndMemoryPrompt(string StablePrompt, string VolatilePrompt, IReadOnlyList<Guid>? InjectedMemoryIds = null);
