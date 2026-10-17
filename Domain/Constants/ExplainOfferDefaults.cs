// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Dosage and wording constants of the "explain briefly first" gate offer: how often a topic may be offered, how
/// many ignored offers in a row silence it, how long an offer waits for its answer, the keys of the counter row's JSON column, and the model-facing
/// note of the turn that follows an accepted offer. The user-facing chip label is a localized text
/// (ExplainOfferTexts); everything here is internal and stays English.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ExplainOfferDefaults
{
    public const int OfferCooldownHours = 24;

    public const int IgnoredOffersBeforeLongSnooze = 3;

    public const int LongSnoozeDays = 30;

    /// <summary>
    /// How long an offer waits for its answer. An offer older than this when the next message of the conversation
    /// arrives is settled as expired (ignored) and never accepted: the chip belongs to a question the user has long
    /// moved past.
    /// </summary>
    public const int PendingOfferMaxAgeMinutes = 10;

    public const char DedupKeySeparator = ':';

    public const string LevelProperty = "level";

    public const string GateSkillProperty = "gateSkill";

    public const string TokenFingerprintProperty = "tokenFingerprint";

    public const string OutcomeProperty = "outcome";

    public const string ExplanationLevel = KnowledgeHappenLevels.Effects;

    public const string AcceptedNoteTemplate =
        "The user accepted your offer to explain first, before deciding on the action you just asked about. " +
        "Explain it briefly in the user's language (a few short sentences; do not walk through every control), " +
        "using ONLY the curated content below. Do not call any skill in this turn and do not confirm, run or cancel " +
        "the held action. End with one short sentence asking whether to proceed with that action, so the user can " +
        "answer yes or no.\n\n{0}\n\nCurated content:\n{1}";
}
