// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Reserved language keys for phrase groups, taken from ISO 639-2 so they can never collide with a
/// real two-letter language tag. Multiple marks a phrase that is written identically in every
/// language (smtp, token, import); Undetermined marks one whose language has not been assigned yet
/// and is the safe default, because a phrase without a language is searched in every language.
/// </summary>
public static class SkillPhraseLanguages
{
    public const string Multiple = "mul";

    public const string Undetermined = "und";
}
