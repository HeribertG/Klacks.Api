// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants for adapting skill result instructions to voice turns.
/// </summary>
/// <param name="DetailCoverageMarker">Phrase identifying the detail-coverage instruction inside knowledge skill results</param>
/// <param name="VoiceAnswerOverride">Instruction appended to knowledge skill results on voice turns to supersede detail coverage</param>
namespace Klacks.Api.Domain.Constants;

public static class VoiceModeInstructionConstants
{
    public const string DetailCoverageMarker = "cover EVERY element";

    public const string VoiceAnswerOverride =
        "\n[VOICE MODE] This is a spoken conversation turn: the detail-coverage instruction above does not apply. " +
        "Give the two or three most important points in flowing spoken sentences without any markdown structure, " +
        "and offer to go deeper instead of walking through every element.";

    public const string SpokenAnswerDirective =
        "\n\n=== VOICE MODE (ACTIVE THIS TURN) ===\n" +
        "Your answer will be read aloud by text-to-speech. Formulate it the way a person SPEAKS, not writes:\n" +
        "- NO markdown of any kind: no headers, no bold, no bullet or numbered lists, no tables, no emojis.\n" +
        "- Never write '1.', '2.' — enumerate the spoken way in the user's language (German: 'Erstens ...', 'Zweitens ...' or 'zum einen ..., zum anderen ...').\n" +
        "- Short flowing sentences with varied length; commas and full stops become audible pauses.\n" +
        "- Pick the two or three most important points and offer to go deeper, instead of listing everything.\n" +
        "- Read numbers, dates and times the way a person would say them aloud.\n" +
        "This section overrides ALL earlier formatting instructions for this turn.";
}
