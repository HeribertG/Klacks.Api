// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Result of a replacement search for one slot: the eligible candidates (best-first) and the
/// excluded ones with reasons. Shared by the find_replacement skill and the cover_absence flow.
/// </summary>
/// <param name="Eligible">Rule-compliant candidates, ranked best-first</param>
/// <param name="Excluded">Candidates that cannot cover the slot, with reason</param>
public sealed record ReplacementSearchResult(
    IReadOnlyList<ReplacementCandidate> Eligible,
    IReadOnlyList<ExcludedCandidate> Excluded);
