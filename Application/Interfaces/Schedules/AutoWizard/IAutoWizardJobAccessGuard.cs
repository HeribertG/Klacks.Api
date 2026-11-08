// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.Schedules.AutoWizard;

/// <summary>
/// Single rule for who may follow, query or cancel a wizard job by id (AutoWizard status and cancel endpoints, the
/// AutoWizard job hub and the job skills): a caller unrestricted by group visibility may address every job; a restricted
/// caller only AutoWizard jobs whose planning group and employees are visible to them. A refused job must be answered like
/// an unknown one. Jobs of the developer-dropdown wizards are additionally reserved to admins by the job skills.
/// </summary>
public interface IAutoWizardJobAccessGuard
{
    /// <param name="jobId">The job the caller addresses</param>
    /// <param name="cancellationToken">Cancels the visibility lookup</param>
    Task<bool> CanAccessAsync(Guid jobId, CancellationToken cancellationToken = default);
}
