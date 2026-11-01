// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// What <see cref="UncoveredAbsenceEntries"/> needs beyond the timeline to judge and name a work on an absence.
/// </summary>
/// <param name="OnCallBreakIds">Ids of the breaks that stand for an on-call absence</param>
/// <param name="ReplacementWindows">Handed-over intervals per work id</param>
/// <param name="ShiftLabels">Abbreviation (or name when it has none) per shift id</param>
/// <param name="AbsenceNames">Name of the absence type per break id</param>
public sealed record UncoveredAbsenceLookup(
    IReadOnlySet<Guid> OnCallBreakIds,
    IReadOnlyDictionary<Guid, IReadOnlyList<ReplacementInterval>> ReplacementWindows,
    IReadOnlyDictionary<Guid, string> ShiftLabels,
    IReadOnlyDictionary<Guid, MultiLanguage> AbsenceNames);
