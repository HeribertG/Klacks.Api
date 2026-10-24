// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the conversation scope key the pending order-review draft is stored under. In the browser chat the skill
/// execution context carries the conversation id in SessionId, so concurrent conversations stay separated; transports
/// that leave it unset fall back to a fixed constant distinct from other intake flows. The string overload exists so
/// the toolset policy, which only holds the raw conversation id, derives the very same key the skills write under.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills.OrderReview;

internal static class OrderReviewDraftScope
{
    public static string ConversationKey(SkillExecutionContext context) => ConversationKey(context.SessionId);

    public static string ConversationKey(string? sessionId) =>
        string.IsNullOrWhiteSpace(sessionId) ? OrderReviewDefaults.DraftScopeFallbackKey : sessionId!;
}
