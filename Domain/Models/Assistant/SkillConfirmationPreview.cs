// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Server-computed facts about a held action that its confirmation request carries, or the reason the action is refused
/// before any confirmation is requested.
/// </summary>
/// <param name="Text">The preview text, or the refusal message when IsRefusal is set</param>
/// <param name="IsRefusal">True when the action cannot run at all; the gate then issues no confirmation token</param>

namespace Klacks.Api.Domain.Models.Assistant;

public record SkillConfirmationPreview(string Text, bool IsRefusal)
{
    public static SkillConfirmationPreview Show(string text) => new(text, false);

    public static SkillConfirmationPreview Refuse(string reason) => new(reason, true);
}
