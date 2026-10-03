// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared checks of the planning-constraint handlers: turns validator errors into a 400, a missing row into a
/// 404 and a concurrent change into a 409, so every handler reports the same way.
/// </summary>

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Application.Handlers.PlanningConstraints;

internal static class PlanningConstraintGuard
{
    public static void EnsureValid(IPlanningConstraintValidator validator, PlanningConstraint constraint)
    {
        var result = validator.Validate(constraint);
        if (!result.IsValid)
        {
            throw new InvalidRequestException(string.Join(" ", result.Errors));
        }
    }

    /// <summary>
    /// Commits through the unit of work and turns a row-version conflict into a 409 (ConflictException is rethrown
    /// unchanged by BaseHandler, a ConcurrencyException would become a 400).
    /// </summary>
    /// <summary>Commits and drops the cached "any approved constraint" answer (the approved set may have changed).</summary>
    public static async Task SaveAsync(IUnitOfWork unitOfWork, IPlanningConstraintPresence presence)
    {
        await SaveAsync(unitOfWork);
        presence.Invalidate();
    }

    public static async Task SaveAsync(IUnitOfWork unitOfWork)
    {
        try
        {
            await unitOfWork.CompleteAsync();
        }
        catch (ConcurrencyException ex)
        {
            throw new PlanningConstraintConcurrencyException(ex);
        }
    }

    /// <summary>
    /// Refuses (400) a constraint whose scope target or scenario does not exist; a rule against a missing target
    /// would be stored as effective but apply to nobody.
    /// </summary>
    public static async Task EnsureReferencesExistAsync(
        IPlanningConstraintReferenceReader references, PlanningConstraint constraint, CancellationToken cancellationToken)
    {
        if (!await references.ScopeTargetExistsAsync(constraint.ScopeType, constraint.ScopeId, cancellationToken))
        {
            throw new InvalidRequestException($"The {constraint.ScopeType} scope target {constraint.ScopeId} does not exist.");
        }

        if (constraint.AnalyseToken.HasValue
            && !await references.ActiveScenarioExistsAsync(constraint.AnalyseToken.Value, cancellationToken))
        {
            throw new InvalidRequestException($"No active scenario exists for analyse token {constraint.AnalyseToken}.");
        }
    }

    public static async Task<PlanningConstraint> GetExistingAsync(
        IPlanningConstraintRepository repository, Guid id, CancellationToken cancellationToken)
    {
        return await repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Planning constraint with ID {id} not found.");
    }
}
