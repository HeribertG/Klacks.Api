// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Retires learned artefacts that went unused or proved unhelpful. The half of the loop that makes the
/// other half safe: without it every wrong lesson would stay in the index forever.
/// </summary>
namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillLearningPruner
{
    /// <summary>
    /// Judges every activated artefact and returns how many were retired.
    /// </summary>
    Task<int> RunAsync(CancellationToken cancellationToken = default);
}
