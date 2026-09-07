// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Chip values that hand the conversation over to a follow-up recipe. Each one must match that
/// recipe's own keyword trigger verbatim — the chip is sent as a fresh user message and resolved
/// like any other, so a phrase that drifts from the trigger silently hands over to nothing.
/// Verified against recipe-seeds.json: create-shift-order needs a "neu" word start plus the
/// substring "bestellung"; create-group needs an "erstell" word start plus the substring "gruppe".
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class SetupHandoffPhrases
{
    public const string CreateShiftOrder = "Neue Bestellung erstellen";

    public const string CreateGroup = "Erstelle eine neue Gruppe";
}
