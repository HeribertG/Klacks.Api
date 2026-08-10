// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the conversation scope key the pending planning-profile draft is stored under. In the
/// browser chat the skill execution context carries the conversation id in SessionId, so concurrent
/// scopes stay separated; transports that leave it unset (scheduled tasks, MCP) fall back to a fixed
/// constant distinct from other intake flows — one admin runs one planning-profile setup at a time.
/// The string overload exists so the toolset assembler, which only holds the raw conversation id,
/// derives the very same key the skills write under instead of re-implementing the fallback.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills.PlanningProfile;

internal static class PlanningProfileDraftScope
{
    private const string DefaultConversationKey = "planning-profile-setup";

    public static string ConversationKey(SkillExecutionContext context) => ConversationKey(context.SessionId);

    public static string ConversationKey(string? sessionId)
    {
        return string.IsNullOrWhiteSpace(sessionId)
            ? DefaultConversationKey
            : sessionId!;
    }
}
