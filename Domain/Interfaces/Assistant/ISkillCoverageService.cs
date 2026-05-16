// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Computes the coverage metric described in roadmap section 10.3 (S10):
/// fraction of the documented disponent use-cases that have at least one skill mapped
/// to them. Source of truth is docs/klacksy-usecases.md (status column ✅ / partial / missing).
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillCoverageService
{
    Task<SkillCoverageReport> ComputeAsync(CancellationToken cancellationToken = default);
}

public sealed record SkillCoverageReport(
    int Total,
    int Covered,
    int Partial,
    int Missing,
    double CoveragePercent,
    DateTime ComputedAtUtc);
