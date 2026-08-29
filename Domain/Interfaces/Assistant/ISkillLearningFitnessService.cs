// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Oracle O3: measures how useful each activated learning artefact has actually been, and records the
/// answer per calendar week so the admin card can show a course rather than a single number.
/// </summary>
namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillLearningFitnessService
{
    /// <summary>
    /// Measures every activated artefact and returns how many were measured.
    /// </summary>
    Task<int> RunAsync(CancellationToken cancellationToken = default);
}
