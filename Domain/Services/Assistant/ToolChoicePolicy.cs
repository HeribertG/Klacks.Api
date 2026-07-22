// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single source of truth for the mutation-guard tool_choice decision, extracted verbatim from the
/// streaming and non-streaming chat loops so both paths share one predicate. A tool call is forced
/// ("required") when a recipe step is being forced, or when a state-changing / navigation / pending-
/// confirmation intent is present and no tool has run yet this turn; otherwise the model keeps its
/// default behaviour (the wire value stays null, i.e. tool_choice is omitted, not "auto").
/// </summary>
/// <param name="forceRecipe">A deterministic recipe step is forcing a specific skill this iteration.</param>
/// <param name="isMutationIntent">The user message expresses a state-changing intent.</param>
/// <param name="isNavigationIntent">The user message expresses a navigation intent.</param>
/// <param name="forceConfirmation">A pending confirmation is being resolved this turn.</param>
/// <param name="toolCallCount">Number of tool calls already executed this turn.</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ToolChoicePolicy
{
    public static bool ShouldForceToolCall(
        bool forceRecipe,
        bool isMutationIntent,
        bool isNavigationIntent,
        bool forceConfirmation,
        int toolCallCount)
    {
        return forceRecipe
            || ((isMutationIntent || isNavigationIntent || forceConfirmation) && toolCallCount == 0);
    }

    public static string? ResolveToolChoice(
        bool forceRecipe,
        bool isMutationIntent,
        bool isNavigationIntent,
        bool forceConfirmation,
        int toolCallCount)
    {
        return ShouldForceToolCall(forceRecipe, isMutationIntent, isNavigationIntent, forceConfirmation, toolCallCount)
            ? MutationGuardConstants.ToolChoiceRequired
            : null;
    }
}
