// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Server-computed facts about a held action that its confirmation request carries, or the reason the action is refused
/// before any confirmation is requested.
/// </summary>
/// <param name="Text">The preview text, or the refusal message when IsRefusal is set</param>
/// <param name="IsRefusal">True when the action cannot run at all; the gate then issues no confirmation token</param>
/// <param name="Binding">The facts the preview shows, stored with the token so the confirmed replay can refuse when they changed; null when the skill does not pin its confirmation</param>

namespace Klacks.Api.Domain.Models.Assistant;

public record SkillConfirmationPreview(string Text, bool IsRefusal, ConfirmationBinding? Binding = null)
{
    public static SkillConfirmationPreview Show(string text) => new(text, false);

    public static SkillConfirmationPreview Show(string text, ConfirmationBinding binding) => new(text, false, binding);

    public static SkillConfirmationPreview Refuse(string reason) => new(reason, true);
}
