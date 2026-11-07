// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Limits and fixed values of creating stand-ins in bulk: at most MaxBulkCount per request, a name pattern of at most
/// MaxNamePatternLength characters that holds the IndexPlaceholder exactly once (the expanded name then fits the 100
/// characters of Client.Name for any int index), and every qualification given to a new stand-in is held at
/// QualificationLevel, so the stand-in can take every service that requires it.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class StandInConstants
{
    public const int MaxBulkCount = 50;

    public const string IndexPlaceholder = "{n}";

    public const int MaxNamePatternLength = 90;

    public const QualificationLevel QualificationLevel = Enums.QualificationLevel.Expert;
}
