// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The assistant turn a goldset item corrects. The replay builds an in-memory AssistantLastAction from
/// it (never a table row) and seeds the conversation history with it, so a correction item measures the
/// same anchor the live pipeline would have had.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public class TurnGoldsetPreviousTurn
{
    /// <summary>The user message that produced the wrong call.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Name of the skill the assistant called on that turn.</summary>
    public string CalledSkill { get; set; } = string.Empty;

    /// <summary>Arguments of that call, verbatim as the model produced them. Empty when irrelevant.</summary>
    public Dictionary<string, string> Arguments { get; set; } = new();

    /// <summary>
    /// Result data of that call as the skill returned it (PascalCase property names, the shape
    /// LLMFunctionExecutor serializes). Carries the created entity id for a create/delete undo pair.
    /// </summary>
    public Dictionary<string, string> ResultData { get; set; } = new();

    /// <summary>
    /// User-facing label of the called skill, as the toolset of that turn described it. The correction
    /// note names this instead of the internal skill name. There is deliberately no risk field: whether
    /// a call was read-only is derived from the skill name exactly as the live write point derives it
    /// (ReadOnlySkillPrefixes), so a goldset item cannot declare a classification production would not
    /// have produced.
    /// </summary>
    public string? SkillDisplayLabel { get; set; }

    /// <summary>Short excerpt of the assistant's answer on that turn, used for the history and the note.</summary>
    public string? AssistantAnswerExcerpt { get; set; }
}
