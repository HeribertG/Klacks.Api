// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies the pending description proposals of the optimizer, but only those that leave every golden
/// case routing as before. A description moves a whole skill vector, so an unguarded change can break
/// skills nobody proposed anything for.
/// </summary>
namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillDescriptionSharpener
{
    /// <summary>
    /// Returns how many proposals were applied automatically and how many were withheld.
    /// </summary>
    Task<(int Applied, int Blocked)> RunAsync(CancellationToken cancellationToken = default);
}
