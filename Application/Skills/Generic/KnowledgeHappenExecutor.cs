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
        "Curated Klacks knowledge. Answer the user's question using ONLY this content, in the user's language. " +
        "Bracketed label anchors like (de: ..., en: ..., fr: ..., it: ...) are internal translation aids: " +
        "use ONLY the label matching the user's language and NEVER print the anchor lists themselves. " +
        "Backticked DOM element ids (e.g. `schedule-prev-btn`) are internal anchors for navigation and highlighting: " +
        "NEVER mention them in your answer — refer to every control only by its visible label. " +
        "Internal entity or technical names (e.g. Work, Break, Expenses, WorkChange, BreakPlaceholder, AnalyseToken, DayLock, " +
        "OriginalOrder, SealedOrder, OriginalShift, SplitShift) are unknown to users: NEVER use them — speak of Diensten, Absenzen, " +
        "Spesen, Korrekturen, vorgeplanten Absenzen, Szenarien, Tagessperren, Bestellungen, versiegelten Bestellungen, planbaren " +
        "Schichten and Teilstücken in the user's language. " +
        "When the user asks for a detailed explanation or how to create/edit a record, cover EVERY element, card and control " +
        "the content describes for that page or mask, in its visible order — never summarize controls away or stop after the first fields. " +
        "If the question goes beyond this content, say so instead of inventing details.";

    private const string LevelInstruction =
        " This is the '{0}' depth of a multi-level explanation. If the user wants a shorter overview " +
        "or more detail, call this skill again with the level parameter set to one of: short, elements, effects.";

    private readonly IAgentMemoryRepository _memoryRepository;

    public KnowledgeHappenExecutor(IAgentMemoryRepository memoryRepository)
    {
        _memoryRepository = memoryRepository;
    }

    public async Task<SkillResult> ExecuteAsync(
        KnowledgeHappenConfig config,
        Dictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
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

        var requestedLevel = ExtractRequestedLevel(parameters);
        var levelContent = requestedLevel == null
            ? null
            : ExtractLevelSection(memory.Content, requestedLevel);

        if (levelContent == null)
        {
            return SkillResult.SuccessResult(
                new { Knowledge = memory.Content },
                AnswerInstruction);
        }

        return SkillResult.SuccessResult(
            new { Knowledge = levelContent, Level = requestedLevel },
            AnswerInstruction + string.Format(LevelInstruction, requestedLevel));
    }

    private static string? ExtractRequestedLevel(Dictionary<string, object>? parameters)
    {
        if (parameters == null ||
            !parameters.TryGetValue(KnowledgeHappenLevels.ParameterName, out var raw))
        {
            return null;
        }

        var value = raw?.ToString()?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return KnowledgeHappenLevels.All.FirstOrDefault(l =>
            string.Equals(l, value, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ExtractLevelSection(string content, string level)
    {
        var markers = FindLevelMarkers(content);
        if (markers.Count == 0)
        {
            return null;
        }

        var index = markers.FindIndex(m =>
            string.Equals(m.Level, level, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return null;
        }

        var sectionStart = markers[index].Position;
        var sectionEnd = index + 1 < markers.Count ? markers[index + 1].Position : content.Length;

        var preamble = content[..markers[0].Position].Trim();
        var section = content[sectionStart..sectionEnd].Trim();

        return preamble.Length > 0 ? preamble + "\n\n" + section : section;
    }

    private static List<(int Position, string Level)> FindLevelMarkers(string content)
    {
        var markers = new List<(int, string)>();
        var searchFrom = 0;

        while (true)
        {
            var start = content.IndexOf(KnowledgeHappenLevels.MarkerPrefix, searchFrom, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
            {
                break;
            }

            var end = content.IndexOf(KnowledgeHappenLevels.MarkerSuffix, start, StringComparison.Ordinal);
            if (end < 0)
            {
                break;
            }

            var level = content[(start + KnowledgeHappenLevels.MarkerPrefix.Length)..end].Trim();
            markers.Add((start, level));
            searchFrom = end + KnowledgeHappenLevels.MarkerSuffix.Length;
        }

        return markers;
    }
}
