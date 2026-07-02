// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single source of the lock-guidance note attached to navigation results for shifts that were
/// cloned from a sealed order (OriginalShift/SplitShift). Only states the status fact and points
/// the model to its explain_shift_lifecycle_order_to_shift knowledge — per-field editability
/// rules are deliberately not duplicated here.
/// </summary>
/// <param name="status">Lifecycle status of the shift the user is navigating to</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant.Guidance;

public static class ShiftLockGuidanceText
{
    public static string? ForStatus(ShiftStatus status) => status switch
    {
        ShiftStatus.OriginalShift or ShiftStatus.SplitShift =>
            $"This shift's Status is {status}: it was cloned from an already-sealed order, so several of " +
            "its fields are read-only. You MUST proactively tell the user, in plain business terms, that " +
            "this shift originates from a sealed order and some fields therefore cannot be edited. Never " +
            "invite the user to change fields on this page without that warning. Before describing any " +
            "specific field as editable, consult your explain_shift_lifecycle_order_to_shift knowledge " +
            "for exactly which fields remain frozen versus still editable — do not guess. Do not mention " +
            "internal identifiers to the user (no status enum names, no knowledge file names).",
        _ => null
    };
}
