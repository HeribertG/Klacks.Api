// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Repository for WizardTrainingRun history.
 * Used by the benchmark service (write) and the admin training endpoints (read).
 */

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IWizardTrainingRepository
{
    /// <summary>Persist a benchmark result.</summary>
    Task AddAsync(WizardTrainingRun run, CancellationToken ct);

    /// <summary>Return the N most recent runs, newest first.</summary>
    Task<IReadOnlyList<WizardTrainingRun>> GetRecentAsync(int limit, CancellationToken ct);

    /// <summary>
    /// Return the best run by Stage2Score among runs that have zero hard violations.
    /// Null if no qualifying run exists.
    /// </summary>
    Task<WizardTrainingRun?> GetBestAsync(CancellationToken ct);
}
