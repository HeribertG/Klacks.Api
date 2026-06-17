// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of <see cref="IEligibilityMatrixBuilder"/>. Carries the set of ineligible
/// (agent, shift, date) assignments for the optimizer veto plus the per-assignment qualification
/// gaps and qualification display metadata for the frontend gap report. An empty matrix means
/// "everyone is eligible" — the default for plans without mandatory shift qualifications.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class EligibilityMatrix
{
    public required IReadOnlySet<(string AgentId, Guid ShiftId, DateOnly Date)> Ineligible { get; init; }

    public required IReadOnlyDictionary<(string AgentId, Guid ShiftId, DateOnly Date), IReadOnlyList<QualificationGap>> Gaps { get; init; }

    public required IReadOnlyDictionary<Guid, QualificationInfo> QualificationInfo { get; init; }

    /// <summary>Shift display names (name, or abbreviation as fallback) keyed by shift id, for the report.</summary>
    public required IReadOnlyDictionary<Guid, string> ShiftNames { get; init; }

    public static EligibilityMatrix Empty { get; } = new()
    {
        Ineligible = new HashSet<(string, Guid, DateOnly)>(),
        Gaps = new Dictionary<(string, Guid, DateOnly), IReadOnlyList<QualificationGap>>(),
        QualificationInfo = new Dictionary<Guid, QualificationInfo>(),
        ShiftNames = new Dictionary<Guid, string>(),
    };
}
