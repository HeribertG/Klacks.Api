// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// CommentParams keys of the qualification schedule-validation entries (pre-commit guardrail and live error list).
/// MinLevel and HeldLevel carry the QualificationLevel enum name, LevelDelta the number of levels missing.
/// </summary>
public static class QualificationValidationParamKeys
{
    public const string QualificationId = "qualificationId";
    public const string MinLevel = "minLevel";
    public const string HeldLevel = "heldLevel";
    public const string LevelDelta = "levelDelta";
    public const string ValidUntil = "validUntil";
}
