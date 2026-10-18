// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Dosage of the UI explain offers (T5). One offer per account: an answer (yes or no) mutes the topic for good, the
/// second "later" mutes it as well, and so does the third time the offer is shown without a choice (a toast closed
/// with X or pushed away by another toast sends nothing, so without this cap it would come back every session).
/// OfferedLevels is the single switch for the audience: owner default 2026-10-18 is beginners only.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class UiExplainOfferDefaults
{
    public const int MaxLaterBeforeMute = 2;

    public const int MaxShownBeforeMute = 3;

    public const char DedupKeySeparator = ':';

    public static readonly IReadOnlySet<ExplainExperienceLevel> OfferedLevels =
        new HashSet<ExplainExperienceLevel> { ExplainExperienceLevel.Beginner };
}
