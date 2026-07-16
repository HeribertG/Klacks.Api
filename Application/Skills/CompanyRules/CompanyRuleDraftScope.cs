// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the conversation scope key the pending company-rule draft is stored under. The skill
/// execution context in the Klacksy chat flow does not carry a conversation id (SessionId is unset there),
/// so the draft is keyed by user plus a fixed constant — one admin runs one intake at a time. When a
/// SessionId is present (scheduled or planning flows) it is used so concurrent scopes stay separated.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills.CompanyRules;

internal static class CompanyRuleDraftScope
{
    private const string DefaultConversationKey = "company-rule-intake";

    public static string ConversationKey(SkillExecutionContext context)
    {
        return string.IsNullOrWhiteSpace(context.SessionId)
            ? DefaultConversationKey
            : context.SessionId!;
    }
}
