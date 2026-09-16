// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One tool call of the previous assistant turn, as much of it as the correction path needs: the skill
/// name to exclude from the next toolset, the arguments and the result data an undo would replay, and
/// whether it was a write at all.
/// </summary>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed class AssistantLastActionCall
{
    public string SkillName { get; set; } = string.Empty;

    /// <summary>
    /// User-facing label of the skill, captured from the toolset of the turn that made the call. The
    /// correction note and the clarification name this instead of the internal snake_case name - and it
    /// has to be captured HERE, because by the time the correction turn runs the skill is excluded from
    /// the toolset and can no longer be looked up. Null when the turn had no description for it; the
    /// caller then falls back to MutationGuardConstants.RedactedInternalIdentifier, never to the name.
    /// </summary>
    public string? SkillDisplayLabel { get; set; }

    /// <summary>Serialized invocation arguments, capped at GracefulCorrectionDefaults.CallJsonMaxLength.</summary>
    public string ArgumentsJson { get; set; } = Constants.GracefulCorrectionDefaults.EmptyJsonObject;

    /// <summary>
    /// Serialized result data of the call (PascalCase keys, the shape LLMFunctionExecutor produces),
    /// capped the same way. Carries the created entity id a create/delete undo pair needs. Empty when
    /// the skill returned no data.
    /// </summary>
    public string ResultDataJson { get; set; } = Constants.GracefulCorrectionDefaults.EmptyJsonObject;

    /// <summary>
    /// Whether the call was read-only, resolved from ReadOnlySkillPrefixes - the same source the
    /// multi-turn loop uses to decide whether a skill may repeat. The undo offer needs nothing finer.
    /// </summary>
    public bool IsReadOnly { get; set; }

    public bool Success { get; set; }
}
