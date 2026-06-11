// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Generic executor for curated knowledge skills (handlerType 'knowledge-happen'): loads the
/// seeded KlacksyKnowledge happen from agent_memories by its key and returns the markdown
/// content as the tool result, so the LLM answers page and concept questions from curated
/// facts instead of hallucinating. The content is canonical; the LLM translates it into the
/// user's language and keeps the localized UI labels embedded in the text.
/// </summary>
/// <param name="memoryRepository">Loads the seeded knowledge memory by key</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills.Generic;

public class KnowledgeHappenExecutor
{
    private const string AnswerInstruction =
        "Curated Klacks knowledge. Answer the user's question using ONLY this content. " +
        "Translate into the user's language; keep the localized UI labels (de/en/fr/it) for on-screen element names. " +
        "If the question goes beyond this content, say so instead of inventing details.";

    private readonly IAgentMemoryRepository _memoryRepository;

    public KnowledgeHappenExecutor(IAgentMemoryRepository memoryRepository)
    {
        _memoryRepository = memoryRepository;
    }

    public async Task<SkillResult> ExecuteAsync(KnowledgeHappenConfig config, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(config.MemoryKey))
        {
            return SkillResult.Error("Knowledge skill is misconfigured: missing memoryKey in handlerConfig.");
        }

        var memory = await _memoryRepository.GetByKeyAsync(config.MemoryKey, cancellationToken);
        if (memory == null || string.IsNullOrWhiteSpace(memory.Content))
        {
            return SkillResult.Error(
                $"Knowledge entry '{config.MemoryKey}' is not available. The seed may not have run yet.");
        }

        return SkillResult.SuccessResult(
            new { Knowledge = memory.Content },
            AnswerInstruction);
    }
}
