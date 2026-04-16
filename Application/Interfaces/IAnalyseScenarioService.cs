// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Application contract for AnalyseScenario-scoped data operations that
/// require direct EF Core access (cloning, promoting, sweeping soft-deletes).
/// Keeps command handlers free of DataBaseContext.
/// </summary>

namespace Klacks.Api.Application.Interfaces;

public interface IAnalyseScenarioService
{
    /// <summary>
    /// Soft-deletes every row carrying the scenario token (works, work_changes,
    /// expenses, breaks, shifts, schedule_notes) plus any original-side
    /// sub-work or sub-break whose ParentWorkId falls inside the scenario's
    /// work set. This orphan sweep prevents zombie rows from re-appearing as
    /// parentless top-level entries in the next scenario clone.
    /// </summary>
    Task SoftDeleteScenarioDataAsync(Guid token, CancellationToken cancellationToken);
}
