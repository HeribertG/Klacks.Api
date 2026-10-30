// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Stateless helpers that skills and the services they delegate to share when they write planning data: the abbreviation
/// derived from a name, the work time of a start/end pair and the verification re-read of a just-written entity.
/// BaseSkillImplementation exposes the same helpers to the skill classes by delegating here.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

public static class SkillWriteSupport
{
    private const int AbbreviationLength = 3;

    public static string GenerateAbbreviation(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return name.Length >= AbbreviationLength ? name[..AbbreviationLength].ToUpper() : name.ToUpper();
        }

        return string.Concat(parts.Select(p => char.ToUpper(p[0])));
    }

    public static decimal CalculateWorkTime(TimeOnly start, TimeOnly end)
    {
        var duration = end.ToTimeSpan() - start.ToTimeSpan();
        if (duration.TotalHours <= 0)
        {
            duration = duration.Add(TimeSpan.FromHours(DayTimeConstants.HoursPerDay));
        }

        return (decimal)duration.TotalHours;
    }

    /// <summary>
    /// Re-reads a just-written entity straight from the database (no tracking cache) and throws a
    /// <see cref="SkillVerificationException"/> when it is missing or fails the validity check. Intended
    /// to run inside <c>IUnitOfWork.ExecuteInTransactionAsync</c> after the write, so a failed check
    /// aborts the operation and rolls the write back instead of reporting a success that never persisted.
    /// </summary>
    /// <param name="skillName">Name of the calling skill, carried on the exception for diagnostics.</param>
    /// <param name="reread">Fresh database read of the written entity (e.g. repository GetNoTracking).</param>
    /// <param name="isPersisted">Predicate confirming the read entity matches the intended write.</param>
    /// <param name="description">Human-readable description of what was being persisted.</param>
    public static async Task ConfirmPersistedAsync<TEntity>(
        string skillName,
        Func<Task<TEntity?>> reread,
        Func<TEntity, bool> isPersisted,
        string description)
        where TEntity : class
    {
        var persisted = await reread();
        if (persisted is null || !isPersisted(persisted))
        {
            throw new SkillVerificationException(
                skillName,
                $"Database verification failed: {description} could not be confirmed in the database " +
                "after the write — the change was rolled back.");
        }
    }
}
