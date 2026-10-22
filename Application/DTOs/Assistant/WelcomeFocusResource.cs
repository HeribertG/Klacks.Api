// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The single most urgent open state the welcome toast asks about. Carries i18n keys and slot
/// values only, never localized text. ActionRoute is filled for ActionKind "navigate" and null
/// otherwise; ActionPromptKey is filled for ActionKind "prompt" - the i18n key of the chat request the
/// frontend submits when the user picks the action - and null otherwise. ConditionId is informational and null for the fresh setup candidate,
/// which has no ledger row behind it.
/// </summary>

namespace Klacks.Api.Application.DTOs.Assistant;

public class WelcomeFocusResource
{
    public string Kind { get; init; } = string.Empty;

    public string PromptKey { get; init; } = string.Empty;

    public Dictionary<string, string> PromptParams { get; init; } = new();

    public string ActionKind { get; init; } = string.Empty;

    public string ActionLabelKey { get; init; } = string.Empty;

    public string? ActionRoute { get; init; }

    public string? ActionPromptKey { get; init; }

    public Guid? ConditionId { get; init; }
}
