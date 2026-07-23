// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the conversation scope key the pending planning-profile draft is stored under. The skill
/// execution context in the Klacksy chat flow does not carry a conversation id (SessionId is unset there),
/// so the draft is keyed by user plus a fixed constant distinct from other intake flows — one admin runs
/// one planning-profile setup at a time. When a SessionId is present it is used so concurrent scopes stay
/// separated.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills.PlanningProfile;

internal static class PlanningProfileDraftScope
{
    private const string DefaultConversationKey = "planning-profile-setup";

    public static string ConversationKey(SkillExecutionContext context)
    {
        return string.IsNullOrWhiteSpace(context.SessionId)
            ? DefaultConversationKey
            : context.SessionId!;
    }
}
