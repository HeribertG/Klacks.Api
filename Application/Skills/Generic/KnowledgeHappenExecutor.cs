// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Generic executor for curated knowledge skills (handlerType 'knowledge-happen'): loads the
/// seeded KlacksyKnowledge happen from agent_memories by its key and returns the markdown
/// content as the tool result, so the LLM answers page and concept questions from curated
/// facts instead of hallucinating. The content is canonical; the LLM translates it into the
/// user's language and keeps the localized UI labels embedded in the text.
/// </summary>
/// <param name="memoryRepository">Loads the seeded knowledge memory by key</param>
/// <param name="logger">Warns when curated content still carried internal entity names that had to be replaced</param>
/// <param name="depth">Explanation depth of the caller: level effects always adds the pitfalls stage, elements adds it
/// for advanced users only, short never; null is treated like a beginner</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Skills.Generic;

public class KnowledgeHappenExecutor
{
    public const string AnswerInstruction =
        "Curated Klacks knowledge. Answer the user's question using ONLY this content, in the user's language. " +
        "Bracketed label anchors like (de: ..., en: ..., fr: ..., it: ...) are internal translation aids: " +
        "use ONLY the label matching the user's language and NEVER print the anchor lists themselves. " +
        "Backticked DOM element ids (e.g. `schedule-prev-btn`) are internal anchors for navigation and highlighting: " +
        "NEVER mention them in your prose — refer to every control only by its visible label. " +
        "EXCEPTION — scroll markers: whenever your answer covers TWO OR MORE sections or cards of the current " +
        "page, you MUST insert the marker [SCROLL:anchor-id] immediately before each section's sentences, " +
        "using that section's backticked anchor id from this content (e.g. [SCROLL:settings-general]). " +
        "This is not optional: the client hides the marker and scrolls the page along so the user sees the " +
        "section being explained; without the markers the user stares at the wrong part of the page. " +
        "Emit a marker only for anchor ids that appear in this content, at most one marker per section, " +
        "and none when you explain a single topic — EXCEPT when the user asks WHERE one control, card or area is " +
        "or how to get to it: then insert exactly one [SCROLL:anchor-id] marker for that element, using its backticked " +
        "anchor id from this content, immediately before the sentence that names it. " +
        "Internal entity or technical names (e.g. Work, Break, Expenses, WorkChange, BreakPlaceholder, AnalyseToken, DayLock, " +
        "OriginalOrder, SealedOrder, OriginalShift, SplitShift) are unknown to users: NEVER use them — speak of Diensten, Absenzen, " +
        "Spesen, Korrekturen, vorgeplanten Absenzen, Szenarien, Tagessperren, Bestellungen, versiegelten Bestellungen, planbaren " +
        "Schichten and Teilstücken in the user's language. " +
        "When the user asks for a detailed explanation or how to create/edit a record, " +
        VoiceModeInstructionConstants.DetailCoverageMarker + ", card and control " +
        "the content describes for that page or mask, in its visible order — never summarize controls away or stop after the first fields. " +
        "If the question goes beyond this content, say so instead of inventing details.";

    public const string PageAlreadyOpenInstruction =
        " Calling this skill has already taken the user to the page it explains: NEVER describe the click path to it " +
        "(gear icon, menu, settings section, tab) — point at the element with its [SCROLL:anchor-id] marker instead; " +
        "the client scrolls to it and highlights it, so tell the user it is highlighted on their screen.";

    public const string NoPageAccessInstruction =
        " This user has NO access to the page this content describes, so the app did not open it: explain only the " +
        "purpose in one or two sentences, then say that an administrator sets this up and that the user cannot open " +
        "it. NEVER describe a click path, NEVER claim that anything is opened or highlighted, and insert no " +
        "[SCROLL:...] markers.";

    private const string LevelInstruction =
        " This is the '{0}' depth of a multi-level explanation. If the user wants a shorter overview " +
        "or more detail, call this skill again with the level parameter set to one of: short, elements, effects.";

    private const string SectionSeparator = "\n\n";

    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly ILogger<KnowledgeHappenExecutor> _logger;

    public KnowledgeHappenExecutor(
        IAgentMemoryRepository memoryRepository,
        ILogger<KnowledgeHappenExecutor> logger)
    {
        _memoryRepository = memoryRepository;
        _logger = logger;
    }

    public async Task<SkillResult> ExecuteAsync(
        KnowledgeHappenConfig config,
        Dictionary<string, object>? parameters = null,
        ExplainDepthProfile? depth = null,
        KnowledgePageAccess pageAccess = KnowledgePageAccess.NoPage,
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

        var content = SanitizeContent(config.MemoryKey, memory.Content);

        var requestedLevel = ExtractRequestedLevel(parameters);
        var levelContent = requestedLevel == null
            ? null
            : ExtractStages(content, ResolveStages(requestedLevel, depth?.Level ?? ExplainExperienceLevel.Beginner));

        var instruction = AnswerInstruction + NavigationInstruction(pageAccess);

        if (levelContent == null)
        {
            return SkillResult.SuccessResult(
                new { Knowledge = content },
                instruction);
        }

        return SkillResult.SuccessResult(
            new { Knowledge = levelContent.Value.Text, Level = requestedLevel, Stages = levelContent.Value.Stages },
            instruction + string.Format(LevelInstruction, requestedLevel));
    }

    // Without a page the click path may be the only way the user finds the place, so it stays allowed.
    private static string NavigationInstruction(KnowledgePageAccess pageAccess) => pageAccess switch
    {
        KnowledgePageAccess.Open => PageAlreadyOpenInstruction,
        KnowledgePageAccess.Denied => NoPageAccessInstruction,
        _ => string.Empty
    };

    /// <summary>
    /// The curated content of a happen as plain text for a caller that grounds its own model turn in it (the gate explain
    /// offer), cut to the stages of the given level exactly as ExecuteAsync would for this depth. Null when the happen is
    /// not seeded or empty. A body without level markers comes back whole, like ExecuteAsync returns it.
    /// </summary>
    /// <param name="memoryKey">Memory key of the happen, i.e. the knowledge skill's name</param>
    /// <param name="level">One of KnowledgeHappenLevels.All</param>
    /// <param name="depth">Explanation depth of the user; decides whether the pitfalls stage joins the elements level</param>
    /// <param name="cancellationToken">Cancels the memory lookup</param>
    public async Task<string?> ReadKnowledgeAsync(
        string memoryKey, string level, ExplainDepthProfile? depth = null, CancellationToken cancellationToken = default)
    {
        var memory = await _memoryRepository.GetByKeyAsync(memoryKey, cancellationToken);
        if (memory == null || string.IsNullOrWhiteSpace(memory.Content))
        {
            return null;
        }

        var content = SanitizeContent(memoryKey, memory.Content);
        var staged = ExtractStages(content, ResolveStages(level, depth?.Level ?? ExplainExperienceLevel.Beginner));
        return staged?.Text ?? content;
    }

    private static IReadOnlyList<string> ResolveStages(string requestedLevel, ExplainExperienceLevel experience)
    {
        if (string.Equals(requestedLevel, KnowledgeHappenLevels.Effects, StringComparison.Ordinal))
        {
            return [KnowledgeHappenLevels.Effects, KnowledgeHappenLevels.Pitfalls];
        }

        if (string.Equals(requestedLevel, KnowledgeHappenLevels.Elements, StringComparison.Ordinal)
            && experience == ExplainExperienceLevel.Advanced)
        {
            return [KnowledgeHappenLevels.Elements, KnowledgeHappenLevels.Pitfalls];
        }

        return [requestedLevel];
    }

    private static (string Text, string[] Stages)? ExtractStages(string content, IReadOnlyList<string> stages)
    {
        var markers = FindLevelMarkers(content);
        if (markers.Count == 0)
        {
            return null;
        }

        var primary = ExtractLevelSection(content, markers, stages[0]);
        if (primary == null)
        {
            return null;
        }

        var sections = new List<string> { primary };
        var includedStages = new List<string> { stages[0] };
        foreach (var stage in stages.Skip(1))
        {
            var section = ExtractLevelSection(content, markers, stage);
            if (section != null)
            {
                sections.Add(section);
                includedStages.Add(stage);
            }
        }

        var preamble = content[..markers[0].Position].Trim();
        var body = string.Join(SectionSeparator, sections);

        return (preamble.Length > 0 ? preamble + SectionSeparator + body : body, includedStages.ToArray());
    }

    private string SanitizeContent(string memoryKey, string rawContent)
    {
        if (string.Equals(memoryKey, SkillNames.ExplainShiftLifecycle, StringComparison.Ordinal))
        {
            return rawContent;
        }

        var sanitized = KnowledgeContentSanitizer.Sanitize(rawContent);
        if (!ReferenceEquals(sanitized, rawContent))
        {
            _logger.LogWarning(
                "Knowledge happen '{MemoryKey}' contained internal entity names that were replaced " +
                "before reaching the assistant. Fix the seed content.",
                memoryKey);
        }

        return sanitized;
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

    private static string? ExtractLevelSection(string content, List<(int Position, string Level)> markers, string level)
    {
        var index = markers.FindIndex(m =>
            string.Equals(m.Level, level, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return null;
        }

        var sectionStart = markers[index].Position;
        var sectionEnd = index + 1 < markers.Count ? markers[index + 1].Position : content.Length;

        return content[sectionStart..sectionEnd].Trim();
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
