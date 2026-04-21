// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Applies a cached wizard scenario to the DB by materialising its non-locked tokens as Work entities.
/// </summary>
public interface IWizardApplyService
{
    /// <summary>
    /// Materialises the scenario identified by <paramref name="jobId"/>. Returns the list of
    /// newly created Work ids. Throws <see cref="InvalidOperationException"/> when no scenario is cached.
    /// </summary>
    Task<IReadOnlyList<Guid>> ApplyAsync(Guid jobId, CancellationToken ct);
}
