// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared checks of the planning-constraint handlers: turns validator errors into a 400 and a missing row
/// into a 404, so every handler reports the same way.
/// </summary>

using Klacks.Api.Domain.Exceptions;
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

    public static async Task<PlanningConstraint> GetExistingAsync(
        IPlanningConstraintRepository repository, Guid id, CancellationToken cancellationToken)
    {
        return await repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Planning constraint with ID {id} not found.");
    }
}
